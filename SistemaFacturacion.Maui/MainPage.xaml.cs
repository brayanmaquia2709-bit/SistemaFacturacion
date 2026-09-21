using System.Collections.ObjectModel;
using SistemaFacturacion.Core.DTOs;
using SistemaFacturacion.Core.Entities;
using SistemaFacturacion.Maui.Services;

namespace SistemaFacturacion.Maui
{
    public class CarritoItemModel
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;

        public decimal PrecioCOP => PrecioUnitario < 10000 ? PrecioUnitario * 1000 : PrecioUnitario;
        public decimal SubtotalCOP => Subtotal < 10000 ? Subtotal * 1000 : Subtotal;

        public string UnitPriceText => $"$ {PrecioCOP:N0} c/u";
        public string CantidadText => $"x{Cantidad}";
        public string SubtotalText => $"$ {SubtotalCOP:N0} COP";
    }

    public partial class MainPage : ContentPage
    {
        private readonly FacturacionApiService _apiService;
        private readonly ObservableCollection<CarritoItemModel> _carrito = new();
        private List<Cliente> _clientes = new();
        private List<Producto> _productos = new();
        private List<Usuario> _usuarios = new();
        private List<Factura> _facturas = new();
        private bool _filtrandoPorFecha = false;
        private Producto? _productoEnEdicion;
        private Cliente? _clienteEnEdicion;
        private Usuario? _usuarioLogueado;
        private Factura? _ultimaFactura;
        private IDispatcherTimer? _autoRefreshTimer;

        public MainPage()
        {
            InitializeComponent();
            _apiService = new FacturacionApiService();
            CarritoCollectionView.ItemsSource = _carrito;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarDatosAsync();
            FormaPagoPicker.SelectedIndex = 0;
            ActualizarEstadoAcceso();

            if (_usuarioLogueado == null)
            {
                OnAbrirModalLoginClicked(this, EventArgs.Empty);
            }

            IniciarAutoRefresco();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            DetenerAutoRefresco();
        }

        private void IniciarAutoRefresco()
        {
            if (_autoRefreshTimer == null)
            {
                _autoRefreshTimer = Dispatcher.CreateTimer();
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(5);
                _autoRefreshTimer.Tick += async (s, e) =>
                {
                    if (_usuarioLogueado != null && !LoadingSpinner.IsVisible && !ModalClienteOverlay.IsVisible && !ModalProductoOverlay.IsVisible)
                    {
                        await CargarDatosSilenciosoAsync();
                    }
                };
            }
            _autoRefreshTimer.Start();
        }

        private void DetenerAutoRefresco()
        {
            _autoRefreshTimer?.Stop();
        }

        private async Task CargarDatosSilenciosoAsync()
        {
            try
            {
                var nuevosProductos = await _apiService.GetProductosAsync();
                var nuevosClientes = await _apiService.GetClientesAsync();

                if (nuevosProductos.Any())
                {
                    int prevSelectedId = (ProductoPicker.SelectedItem as Producto)?.Id ?? 0;
                    _productos = nuevosProductos;

                    // Actualizar catálogo del Picker y de la cuadrícula POS
                    ProductoPicker.ItemsSource = null;
                    ProductoPicker.ItemsSource = _productos;

                    if (ProductosCatalogCollectionView != null)
                    {
                        ProductosCatalogCollectionView.ItemsSource = null;
                        ProductosCatalogCollectionView.ItemsSource = _productos;
                    }

                    if (AdminProductosCollectionView != null)
                    {
                        AdminProductosCollectionView.ItemsSource = null;
                        AdminProductosCollectionView.ItemsSource = _productos;
                    }

                    if (prevSelectedId > 0)
                    {
                        var prodReencontrado = _productos.FirstOrDefault(p => p.Id == prevSelectedId);
                        if (prodReencontrado != null)
                        {
                            ProductoPicker.SelectedItem = prodReencontrado;
                            OnProductoSelected(ProductoPicker, EventArgs.Empty);
                        }
                    }
                    else if (_productos.Any())
                    {
                        ProductoPicker.SelectedIndex = 0;
                    }
                }

                if (nuevosClientes.Any())
                {
                    int prevSelectedCliId = (ClientePicker.SelectedItem as Cliente)?.Id ?? 0;
                    _clientes = nuevosClientes;

                    ClientePicker.ItemsSource = null;
                    ClientePicker.ItemsSource = _clientes;

                    if (AdminClientesCollectionView != null)
                    {
                        AdminClientesCollectionView.ItemsSource = null;
                        AdminClientesCollectionView.ItemsSource = _clientes;
                    }

                    if (prevSelectedCliId > 0)
                    {
                        var cliReencontrado = _clientes.FirstOrDefault(c => c.Id == prevSelectedCliId);
                        if (cliReencontrado != null)
                        {
                            ClientePicker.SelectedItem = cliReencontrado;
                            OnClienteSelected(ClientePicker, EventArgs.Empty);
                        }
                    }
                    else if (_clientes.Any())
                    {
                        ClientePicker.SelectedIndex = 0;
                    }
                }

                var nuevasFacturas = await _apiService.GetFacturasAsync();
                if (nuevasFacturas.Any())
                {
                    _facturas = nuevasFacturas;
                    AplicarFiltrosFacturas();
                }
                ActualizarDashboardKpis();
            }
            catch
            {
                // Ignorar errores silenciosamente para mantener la fluidez en segundo plano
            }
        }

        private void ActualizarEstadoAcceso()
        {
            bool estaAutenticado = _usuarioLogueado != null;

            MainContentScrollView.IsEnabled = estaAutenticado;
            MainContentScrollView.InputTransparent = !estaAutenticado;
            MainContentScrollView.Opacity = estaAutenticado ? 1.0 : 0.40;

            AbrirLoginBtn.IsVisible = !estaAutenticado;
            CerrarSesionBtn.IsVisible = estaAutenticado;

            if (estaAutenticado)
            {
                bool esAdmin = string.Equals(_usuarioLogueado!.Rol, "Admin", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(_usuarioLogueado.Rol, "Administrador", StringComparison.OrdinalIgnoreCase);

                // --- CONTROLES EXCLUSIVOS DE ADMINISTRADOR ---
                AdminPanelBtn.IsVisible = esAdmin;
                if (ServerConfigBtn != null) ServerConfigBtn.IsVisible = esAdmin;
                if (AddProductBtn != null) AddProductBtn.IsVisible = esAdmin;
                if (EditProductCardBorder != null) EditProductCardBorder.IsVisible = esAdmin;

                if (esAdmin)
                {
                    UserSessionLabel.Text = $"🛡️ Administrador: {_usuarioLogueado.Nombre} (Acceso Total y Configuración)";
                    UserSessionLabel.TextColor = Color.FromArgb("#10B981"); // Verde Esmeralda
                    if (AdminNombreLabel != null) AdminNombreLabel.Text = $"Administrador: {_usuarioLogueado.Nombre}";

                    if (AdminTabBtn != null) AdminTabBtn.IsVisible = true;
                    if (DualTabBtn != null) DualTabBtn.IsVisible = true;

                    // Al ingresar como Administrador, mostrar la Interfaz del Administrador por defecto
                    ActivarVista("Admin");
                }
                else
                {
                    UserSessionLabel.Text = $"👤 Cajero: {_usuarioLogueado.Nombre} (Operativa POS & Facturación)";
                    UserSessionLabel.TextColor = Color.FromArgb("#3B82F6"); // Azul Corporativo

                    if (AdminTabBtn != null) AdminTabBtn.IsVisible = false;
                    if (DualTabBtn != null) DualTabBtn.IsVisible = false;

                    // Al ingresar como Cajero, mostrar exclusivamente la Interfaz del Cajero POS
                    ActivarVista("Cajero");
                }
            }
            else
            {
                AdminPanelBtn.IsVisible = false;
                if (ServerConfigBtn != null) ServerConfigBtn.IsVisible = false;
                if (AddProductBtn != null) AddProductBtn.IsVisible = false;
                if (EditProductCardBorder != null) EditProductCardBorder.IsVisible = false;
                if (AdminTabBtn != null) AdminTabBtn.IsVisible = false;
                if (DualTabBtn != null) DualTabBtn.IsVisible = false;

                UserSessionLabel.Text = "🔒 Sesión Bloqueada: Inicie Sesión para Operar";
                UserSessionLabel.TextColor = Color.FromArgb("#EF4444");
                ActivarVista("Cajero");
            }
        }

        private void OnMostrarCajeroViewClicked(object? sender, EventArgs e)
        {
            ActivarVista("Cajero");
        }

        private async void OnMostrarAdminViewClicked(object? sender, EventArgs e)
        {
            bool esAdmin = _usuarioLogueado != null &&
                (string.Equals(_usuarioLogueado.Rol, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(_usuarioLogueado.Rol, "Administrador", StringComparison.OrdinalIgnoreCase));

            if (!esAdmin)
            {
                await DisplayAlertAsync("Acceso Restringido", "Solo los administradores tienen acceso a la interfaz de administración.", "OK");
                return;
            }

            ActivarVista("Admin");
        }

        private async void OnMostrarVistaDualClicked(object? sender, EventArgs e)
        {
            bool esAdmin = _usuarioLogueado != null &&
                (string.Equals(_usuarioLogueado.Rol, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(_usuarioLogueado.Rol, "Administrador", StringComparison.OrdinalIgnoreCase));

            if (!esAdmin)
            {
                await DisplayAlertAsync("Acceso Restringido", "La vista dual está disponible únicamente para el Administrador.", "OK");
                return;
            }

            ActivarVista("Dual");
        }

        private void ActivarVista(string modo)
        {
            if (Quadrant1 == null || Quadrant2 == null || MainLayoutGrid == null) return;

            if (modo == "Cajero")
            {
                Quadrant1.IsVisible = true;
                Quadrant2.IsVisible = false;
                Grid.SetColumn(Quadrant1, 0);
                Grid.SetColumnSpan(Quadrant1, 2);

                if (CajeroTabBtn != null) { CajeroTabBtn.Opacity = 1.0; CajeroTabBtn.FontAttributes = FontAttributes.Bold; }
                if (AdminTabBtn != null) { AdminTabBtn.Opacity = 0.5; AdminTabBtn.FontAttributes = FontAttributes.None; }
                if (DualTabBtn != null) { DualTabBtn.Opacity = 0.5; DualTabBtn.FontAttributes = FontAttributes.None; }
            }
            else if (modo == "Admin")
            {
                Quadrant1.IsVisible = false;
                Quadrant2.IsVisible = true;
                Grid.SetColumn(Quadrant2, 0);
                Grid.SetColumnSpan(Quadrant2, 2);

                if (CajeroTabBtn != null) { CajeroTabBtn.Opacity = 0.5; CajeroTabBtn.FontAttributes = FontAttributes.None; }
                if (AdminTabBtn != null) { AdminTabBtn.Opacity = 1.0; AdminTabBtn.FontAttributes = FontAttributes.Bold; }
                if (DualTabBtn != null) { DualTabBtn.Opacity = 0.5; DualTabBtn.FontAttributes = FontAttributes.None; }
            }
            else // Dual
            {
                Quadrant1.IsVisible = true;
                Quadrant2.IsVisible = true;
                Grid.SetColumn(Quadrant1, 0);
                Grid.SetColumnSpan(Quadrant1, 1);
                Grid.SetColumn(Quadrant2, 1);
                Grid.SetColumnSpan(Quadrant2, 1);

                if (CajeroTabBtn != null) { CajeroTabBtn.Opacity = 0.5; CajeroTabBtn.FontAttributes = FontAttributes.None; }
                if (AdminTabBtn != null) { AdminTabBtn.Opacity = 0.5; AdminTabBtn.FontAttributes = FontAttributes.None; }
                if (DualTabBtn != null) { DualTabBtn.Opacity = 1.0; DualTabBtn.FontAttributes = FontAttributes.Bold; }
            }
        }

        private void OnAbrirModalAdminClicked(object? sender, EventArgs e)
        {
            AdminServerUrlLabel.Text = FacturacionApiService.GetConfiguredBaseUrl();
            ModalAdminOverlay.IsVisible = true;
        }

        private void OnCerrarModalAdminClicked(object? sender, EventArgs e)
        {
            ModalAdminOverlay.IsVisible = false;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (MainLayoutGrid == null || Quadrant1 == null || Quadrant2 == null || Quadrant3 == null || Quadrant4 == null) return;

            if (width < 900) // Modo Pantalla Angosta (1 sola columna para móviles / tablets verticales)
            {
                MainLayoutGrid.ColumnDefinitions.Clear();
                MainLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                MainLayoutGrid.RowDefinitions.Clear();
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(Quadrant1, 0); Grid.SetRow(Quadrant1, 0);
                Grid.SetColumn(Quadrant2, 0); Grid.SetRow(Quadrant2, 1);
                Grid.SetColumn(Quadrant3, 0); Grid.SetRow(Quadrant3, 2);
                Grid.SetColumn(Quadrant4, 0); Grid.SetRow(Quadrant4, 3);
            }
            else // Modo Escritorio / Tablet Horizontal (2x2 Grid)
            {
                MainLayoutGrid.ColumnDefinitions.Clear();
                MainLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                MainLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                MainLayoutGrid.RowDefinitions.Clear();
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(Quadrant1, 0); Grid.SetRow(Quadrant1, 0);
                Grid.SetColumn(Quadrant2, 1); Grid.SetRow(Quadrant2, 0);
                Grid.SetColumn(Quadrant3, 0); Grid.SetRow(Quadrant3, 1);
                Grid.SetColumn(Quadrant4, 1); Grid.SetRow(Quadrant4, 1);
            }
        }

        private async Task CargarDatosAsync()
        {
            SetLoading(true);
            try
            {
                _clientes = await _apiService.GetClientesAsync();
                _productos = await _apiService.GetProductosAsync();
                _usuarios = await _apiService.GetUsuariosAsync();
                _facturas = await _apiService.GetFacturasAsync();

                ClientePicker.ItemsSource = _clientes;
                ProductoPicker.ItemsSource = _productos;
                if (ProductosCatalogCollectionView != null) ProductosCatalogCollectionView.ItemsSource = _productos;
                UsuariosRapidosPicker.ItemsSource = _usuarios;

                if (AdminProductosCollectionView != null) AdminProductosCollectionView.ItemsSource = _productos;
                if (AdminClientesCollectionView != null) AdminClientesCollectionView.ItemsSource = _clientes;
                if (AdminUsuariosCollectionView != null) AdminUsuariosCollectionView.ItemsSource = _usuarios;
                AplicarFiltrosFacturas();
                ActualizarDashboardKpis();

                if (_clientes.Any() && ClientePicker.SelectedIndex < 0) ClientePicker.SelectedIndex = 0;
                if (_productos.Any() && ProductoPicker.SelectedIndex < 0) ProductoPicker.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudieron cargar los datos de la API: {ex.Message}", "OK");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnProductoCardTapped(object? sender, EventArgs e)
        {
            Producto? prod = (e as TappedEventArgs)?.Parameter as Producto ??
                             (sender as BindableObject)?.BindingContext as Producto;

            if (prod == null) return;

            // Seleccionar en el picker
            ProductoPicker.SelectedItem = prod;

            if (prod.Stock <= 0)
            {
                await DisplayAlertAsync("Stock Agotado", $"El producto '{prod.Nombre}' no cuenta con unidades disponibles.", "OK");
                return;
            }

            int cantidad = 1;
            if (int.TryParse(CantidadEntry.Text, out int cantIngresada) && cantIngresada > 0)
            {
                cantidad = cantIngresada;
            }

            var itemExistente = _carrito.FirstOrDefault(c => c.ProductoId == prod.Id);
            if (itemExistente != null)
            {
                if (prod.Stock < itemExistente.Cantidad + cantidad)
                {
                    await DisplayAlertAsync("Stock Insuficiente", $"No puede agregar más unidades de '{prod.Nombre}'. Stock disponible: {prod.Stock}", "OK");
                    return;
                }
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                _carrito.Add(new CarritoItemModel
                {
                    ProductoId = prod.Id,
                    ProductoNombre = prod.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = prod.Precio
                });
            }

            ActualizarTotales();
        }

        private async void OnRecargarClicked(object? sender, EventArgs e)
        {
            await CargarDatosAsync();
        }

        private void OnClienteSelected(object? sender, EventArgs e)
        {
            if (ClientePicker.SelectedItem is Cliente cliente)
            {
                ClienteBadgeNombre.Text = $"👤 {cliente.Nombre}";
                ClienteBadgeDoc.Text = $"🆔 Documento: {cliente.DocumentoIdentidad}";
                ClienteBadgeEmail.Text = $"📧 Email: {cliente.Email}";
                ClienteBadgeTel.Text = $"📞 Teléfono: {cliente.Telefono}";
                ClienteCardBadge.IsVisible = false;
            }
            else
            {
                ClienteCardBadge.IsVisible = false;
            }
        }

        private void OnProductoSelected(object? sender, EventArgs e)
        {
            if (ProductoPicker.SelectedItem is Producto producto)
            {
                ProductoBadgeNombre.Text = $"📦 {producto.Nombre}";
                ProductoBadgePrecio.Text = $"💰 Precio: {producto.PrecioCOPFormatted}";
                ProductoBadgeCodigo.Text = $"🏷️ SKU: {producto.CodigoBarra} | Categoría: {producto.Categoria}";
                ProductoBadgeStock.Text = $"Stock: {producto.Stock} uds";

                if (producto.Stock > 10)
                {
                    StockBadgeBorder.BackgroundColor = Color.FromArgb("#10B981"); // Emerald Green
                }
                else if (producto.Stock > 0)
                {
                    StockBadgeBorder.BackgroundColor = Color.FromArgb("#F59E0B"); // Amber
                }
                else
                {
                    StockBadgeBorder.BackgroundColor = Color.FromArgb("#EF4444"); // Red
                }

                ProductoCardBadge.IsVisible = false;
            }
            else
            {
                ProductoCardBadge.IsVisible = false;
            }
        }

        private void OnDescuentoChanged(object? sender, EventArgs e)
        {
            ActualizarTotales();
        }

        private async void OnAgregarProductoClicked(object? sender, EventArgs e)
        {
            if (ProductoPicker.SelectedItem is not Producto producto)
            {
                await DisplayAlertAsync("Atención", "Seleccione un producto para agregar.", "OK");
                return;
            }

            if (!int.TryParse(CantidadEntry.Text, out int cantidad) || cantidad <= 0)
            {
                await DisplayAlertAsync("Atención", "Ingrese una cantidad válida mayor a 0.", "OK");
                return;
            }

            if (producto.Stock < cantidad)
            {
                await DisplayAlertAsync("Stock Insuficiente", $"El producto '{producto.Nombre}' solo cuenta con {producto.Stock} unidades en stock.", "OK");
                return;
            }

            var itemExistente = _carrito.FirstOrDefault(c => c.ProductoId == producto.Id);
            if (itemExistente != null)
            {
                if (producto.Stock < itemExistente.Cantidad + cantidad)
                {
                    await DisplayAlertAsync("Stock Insuficiente", $"No puede agregar más unidades. Disponible en stock: {producto.Stock}", "OK");
                    return;
                }
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                _carrito.Add(new CarritoItemModel
                {
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio
                });
            }

            CarritoCollectionView.ItemsSource = null;
            CarritoCollectionView.ItemsSource = _carrito;
            ActualizarTotales();
        }

        private void OnEliminarItemClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CarritoItemModel item)
            {
                _carrito.Remove(item);
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            decimal subtotalBruto = _carrito.Sum(i => i.Subtotal);

            decimal.TryParse(DescuentoEntry.Text, out decimal pctDescuento);
            if (pctDescuento < 0) pctDescuento = 0;
            if (pctDescuento > 100) pctDescuento = 100;

            decimal descuentoMonto = subtotalBruto * (pctDescuento / 100m);
            decimal subtotalNeto = subtotalBruto - descuentoMonto;
            decimal impuestos = subtotalNeto * 0.19m; // 19% IVA Colombia
            decimal total = subtotalNeto + impuestos;

            decimal subtotalBrutoCOP = subtotalBruto < 10000 ? subtotalBruto * 1000 : subtotalBruto;
            decimal totalCOP = total < 10000 ? total * 1000 : total;
            decimal descuentoMontoCOP = descuentoMonto < 10000 ? descuentoMonto * 1000 : descuentoMonto;
            decimal impuestosCOP = impuestos < 10000 ? impuestos * 1000 : impuestos;

            SubtotalLabel.Text = $"$ {subtotalBrutoCOP:N0} COP";
            DescuentoLabel.Text = $"- $ {descuentoMontoCOP:N0} COP ({pctDescuento}%)";
            ImpuestoLabel.Text = $"$ {impuestosCOP:N0} COP";
            TotalLabel.Text = $"$ {totalCOP:N0} COP";
        }

        private async void OnGenerarFacturaClicked(object? sender, EventArgs e)
        {
            if (ClientePicker.SelectedItem is not Cliente cliente)
            {
                await DisplayAlertAsync("Atención", "Debe seleccionar un cliente.", "OK");
                return;
            }

            if (!_carrito.Any())
            {
                await DisplayAlertAsync("Atención", "El carrito de compras está vacío.", "OK");
                return;
            }

            decimal subtotalBruto = _carrito.Sum(i => i.Subtotal);
            decimal.TryParse(DescuentoEntry.Text, out decimal pctDescuento);
            decimal descuentoMonto = subtotalBruto * (pctDescuento / 100m);
            decimal subtotalNeto = subtotalBruto - descuentoMonto;
            decimal total = subtotalNeto + (subtotalNeto * 0.19m);

            _totalActualCobro = total;
            ModalCobroTotalLabel.Text = $"${_totalActualCobro:N2}";
            ModalCobroFormaPagoPicker.SelectedIndex = 0;
            ModalPagaConEntry.Text = _totalActualCobro.ToString("F0");
            CalcularCambio();

            ModalCobroOverlay.IsVisible = true;
        }

        private async void OnDescargarPdfClicked(object? sender, EventArgs e)
        {
            if (_ultimaFactura == null) return;

            SetLoading(true);
            try
            {
                byte[]? pdfBytes = await _apiService.DescargarPdfBytesAsync(_ultimaFactura.Id);
                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    string tempFilePath = Path.Combine(FileSystem.CacheDirectory, $"Factura_{_ultimaFactura.NumeroFactura}.pdf");
                    await File.WriteAllBytesAsync(tempFilePath, pdfBytes);

                    try
                    {
                        await Launcher.Default.OpenAsync(new OpenFileRequest
                        {
                            File = new ReadOnlyFile(tempFilePath)
                        });
                    }
                    catch
                    {
                        string pdfApiUrl = $"http://localhost:5145/api/Facturas/{_ultimaFactura.Id}/pdf";
                        await Launcher.Default.OpenAsync(new Uri(pdfApiUrl));
                    }
                }
                else
                {
                    await DisplayAlertAsync("Atención", "No se pudo generar el archivo PDF de la factura.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error PDF", $"No se pudo abrir el PDF: {ex.Message}", "OK");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnEnviarCorreoClicked(object? sender, EventArgs e)
        {
            if (_ultimaFactura == null) return;

            string clienteEmail = _ultimaFactura.Cliente?.Email ?? "";
            if (string.IsNullOrEmpty(clienteEmail))
            {
                var cli = _clientes.FirstOrDefault(c => c.Id == _ultimaFactura.ClienteId);
                if (cli != null) clienteEmail = cli.Email;
            }

            string emailDestino = await DisplayPromptAsync("Enviar Correo ✉️",
                $"Ingrese la dirección de correo para enviar la factura #{_ultimaFactura.NumeroFactura}:",
                initialValue: clienteEmail, keyboard: Keyboard.Email);

            if (string.IsNullOrWhiteSpace(emailDestino)) return;

            SetLoading(true);
            bool ok = await _apiService.EnviarCorreoAsync(_ultimaFactura.Id, emailDestino);
            SetLoading(false);

            if (ok)
            {
                await DisplayAlertAsync("Correo Enviado ✉️", $"Factura #{_ultimaFactura.NumeroFactura} enviada exitosamente a {emailDestino}", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error Correo", "Ocurrió un error al enviar el correo.", "OK");
            }
        }

        private async void OnWhatsAppClicked(object? sender, EventArgs e)
        {
            if (_ultimaFactura == null) return;

            string rawTelefono = _ultimaFactura.Cliente?.Telefono ?? "";
            if (string.IsNullOrEmpty(rawTelefono))
            {
                var cli = _clientes.FirstOrDefault(c => c.Id == _ultimaFactura.ClienteId);
                if (cli != null) rawTelefono = cli.Telefono;
            }

            string telefonoSoloNumeros = System.Text.RegularExpressions.Regex.Replace(rawTelefono, @"[^\d]", "");
            string pdfUrl = $"http://localhost:5145/api/Facturas/{_ultimaFactura.Id}/pdf";
            string mensajeWa = Uri.EscapeDataString($"Hola, adjunto el resumen de tu Factura electrónica #{_ultimaFactura.NumeroFactura} por un valor total de ${_ultimaFactura.Total:N2}. Puedes descargar tu PDF aquí: {pdfUrl}");

            string waUrl = string.IsNullOrEmpty(telefonoSoloNumeros)
                ? $"https://api.whatsapp.com/send?text={mensajeWa}"
                : $"https://api.whatsapp.com/send?phone={telefonoSoloNumeros}&text={mensajeWa}";

            try
            {
                await Launcher.Default.OpenAsync(new Uri(waUrl));
            }
            catch
            {
                await DisplayAlertAsync("WhatsApp", $"No se pudo abrir WhatsApp automáticamente. Enlace: {waUrl}", "OK");
            }
        }

        private void SetLoading(bool loading)
        {
            LoadingSpinner.IsVisible = loading;
            LoadingSpinner.IsRunning = loading;
            GenerarBtn.IsEnabled = !loading;
        }

        // Login / Autenticación Vendedor
        private void OnAbrirModalLoginClicked(object? sender, EventArgs e)
        {
            bool remember = Preferences.Get("RememberUser", false);
            if (RecordarmeCheckBox != null) RecordarmeCheckBox.IsChecked = remember;
            if (remember && LoginUsernameEntry != null)
            {
                LoginUsernameEntry.Text = Preferences.Get("SavedUsername", "");
            }
            if (LoginPasswordEntry != null)
            {
                LoginPasswordEntry.Text = string.Empty; // NUNCA contraseña por defecto
            }
            ModalLoginOverlay.IsVisible = true;
        }

        private void OnCerrarModalLoginClicked(object? sender, EventArgs e)
        {
            ModalLoginOverlay.IsVisible = false;
        }

        private async void OnCerrarSesionClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync("Cerrar Sesión 🚪", "¿Está seguro que desea cerrar la sesión actual de vendedor?", "Sí, Cerrar Sesión", "Cancelar");
            if (!confirm) return;

            _usuarioLogueado = null;
            _carrito.Clear();
            ActualizarTotales();

            ActualizarEstadoAcceso();
            OnAbrirModalLoginClicked(this, EventArgs.Empty);
        }

        private void OnModalOverlayBackgroundTapped(object? sender, TappedEventArgs e)
        {
            // Intercepta los toques en el fondo oscuro del modal para que no traspasen al contenido inferior
        }

        private void OnUsuarioRapidoSelected(object? sender, EventArgs e)
        {
            if (UsuariosRapidosPicker.SelectedItem is Usuario user)
            {
                LoginUsernameEntry.Text = user.Username;
                LoginPasswordEntry.Text = string.Empty; // NUNCA autocompletar contraseña por seguridad
            }
        }

        private async void OnIngresarLoginClicked(object? sender, EventArgs e)
        {
            string username = LoginUsernameEntry.Text?.Trim() ?? "";
            string password = LoginPasswordEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlertAsync("Atención", "Ingrese usuario y contraseña.", "OK");
                return;
            }

            SetLoading(true);
            var usuario = await _apiService.LoginAsync(username, password);
            SetLoading(false);

            if (usuario != null)
            {
                _usuarioLogueado = usuario;

                if (RecordarmeCheckBox != null && RecordarmeCheckBox.IsChecked)
                {
                    Preferences.Set("RememberUser", true);
                    Preferences.Set("SavedUsername", username);
                }
                else
                {
                    Preferences.Set("RememberUser", false);
                    Preferences.Remove("SavedUsername");
                }

                ActualizarEstadoAcceso();
                ModalLoginOverlay.IsVisible = false;
                await DisplayAlertAsync("Bienvenido 🎉", $"Sesión iniciada como {usuario.Nombre} [{usuario.Rol}]", "OK");
            }
            else
            {
                string currentUrl = FacturacionApiService.GetConfiguredBaseUrl();
                bool config = await DisplayAlertAsync("Error de Conexión / Autenticación",
                    $"Usuario o contraseña incorrectos, o no se pudo conectar a la API en: {currentUrl}\n\n¿Deseas configurar la IP del Servidor ahora?",
                    "Configurar IP 🌐", "Reintentar");

                if (config)
                {
                    OnAbrirModalServerClicked(this, EventArgs.Empty);
                }
            }
        }

        private void ActualizarDashboardKpis()
        {
            if (_facturas == null || _productos == null) return;

            DateTime hoy = DateTime.Today;

            // 1. Ventas de Hoy
            var facturasHoy = _facturas.Where(f => f.Estado != "Anulada" && (f.Fecha.Date == hoy || f.Fecha.ToLocalTime().Date == hoy)).ToList();
            decimal ventasHoyCOP = facturasHoy.Sum(f => f.TotalCOP);

            if (KpiVentasHoyLabel != null) KpiVentasHoyLabel.Text = $"$ {ventasHoyCOP:N0} COP";
            if (KpiVentasHoyTrendLabel != null) KpiVentasHoyTrendLabel.Text = $"{facturasHoy.Count} ventas hoy";

            // 2. Total Facturas / Ventas
            var facturasActivas = _facturas.Where(f => f.Estado != "Anulada").ToList();
            if (KpiPedidosLabel != null) KpiPedidosLabel.Text = facturasActivas.Count.ToString();
            if (KpiPedidosTrendLabel != null) KpiPedidosTrendLabel.Text = $"{_facturas.Count} facturas";

            // 3. Poco Stock (Stock <= 5)
            var prodsPocoStock = _productos.Where(p => p.Stock <= 5).ToList();
            if (KpiPocoStockLabel != null) KpiPocoStockLabel.Text = prodsPocoStock.Count.ToString();
            if (KpiPocoStockSubLabel != null) KpiPocoStockSubLabel.Text = $"{prodsPocoStock.Count} prods";

            // 4. Resumen de Ventas (10 Barras dinámicas según ventas reales)
            var ultimasVentas = facturasActivas.Take(10).Reverse().ToList();
            decimal maxVenta = ultimasVentas.Any() ? ultimasVentas.Max(v => v.TotalCOP) : 1;
            if (maxVenta <= 0) maxVenta = 1;

            BoxView[] barBoxViews = new[] { BarVenta0, BarVenta1, BarVenta2, BarVenta3, BarVenta4, BarVenta5, BarVenta6, BarVenta7, BarVenta8, BarVenta9 };
            for (int i = 0; i < 10; i++)
            {
                if (barBoxViews[i] != null)
                {
                    if (i < ultimasVentas.Count)
                    {
                        double altura = (double)(ultimasVentas[i].TotalCOP / maxVenta) * 65.0 + 10.0;
                        barBoxViews[i].HeightRequest = Math.Min(75.0, Math.Max(10.0, altura));
                        barBoxViews[i].IsVisible = true;
                    }
                    else
                    {
                        barBoxViews[i].HeightRequest = 10;
                    }
                }
            }

            // 5. Estado de Inventario (% productos en stock adecuado)
            int totalProds = _productos.Count;
            int prodsSaludables = _productos.Count(p => p.Stock > 5);
            int pctSaludable = totalProds > 0 ? (int)Math.Round((double)prodsSaludables / totalProds * 100) : 100;

            if (EstadoInventarioLabel != null) EstadoInventarioLabel.Text = $"{pctSaludable}%";
            if (EstadoInventarioEllipse != null)
            {
                if (pctSaludable >= 70) EstadoInventarioEllipse.Stroke = Color.FromArgb("#10B981");
                else if (pctSaludable >= 40) EstadoInventarioEllipse.Stroke = Color.FromArgb("#F59E0B");
                else EstadoInventarioEllipse.Stroke = Color.FromArgb("#EF4444");
            }
        }

        // Modales de Creación Rápida de Clientes y Productos
        private void OnAbrirModalClienteClicked(object? sender, EventArgs e)
        {
            ClienteNombreEntry.Text = "";
            ClienteDocumentoEntry.Text = "";
            ClienteEmailEntry.Text = "";
            ClienteTelefonoEntry.Text = "";
            ClienteDireccionEntry.Text = "";
            ModalClienteOverlay.IsVisible = true;
        }

        private void OnCerrarModalClienteClicked(object? sender, EventArgs e)
        {
            ModalClienteOverlay.IsVisible = false;
        }

        private async void OnGuardarClienteClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ClienteNombreEntry.Text) ||
                string.IsNullOrWhiteSpace(ClienteDocumentoEntry.Text) ||
                string.IsNullOrWhiteSpace(ClienteEmailEntry.Text))
            {
                await DisplayAlertAsync("Atención", "Complete los campos obligatorios (*): Nombre, Documento e Email.", "OK");
                return;
            }

            var nuevoCliente = new Cliente
            {
                Nombre = ClienteNombreEntry.Text.Trim(),
                DocumentoIdentidad = ClienteDocumentoEntry.Text.Trim(),
                Email = ClienteEmailEntry.Text.Trim(),
                Telefono = ClienteTelefonoEntry.Text?.Trim() ?? "",
                Direccion = ClienteDireccionEntry.Text?.Trim() ?? ""
            };

            SetLoading(true);
            var clienteCreado = await _apiService.CrearClienteAsync(nuevoCliente);
            SetLoading(false);

            if (clienteCreado != null)
            {
                ModalClienteOverlay.IsVisible = false;
                await DisplayAlertAsync("Éxito 🎉", $"Cliente '{clienteCreado.Nombre}' registrado correctamente.", "OK");

                await CargarDatosAsync();

                var item = _clientes.FirstOrDefault(c => c.Id == clienteCreado.Id);
                if (item != null) ClientePicker.SelectedItem = item;
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo guardar el cliente.", "OK");
            }
        }

        private void OnAbrirModalProductoClicked(object? sender, EventArgs e)
        {
            ProductoNombreEntry.Text = "";
            ProductoCategoriaEntry.Text = "";
            ProductoPrecioEntry.Text = "";
            ProductoStockEntry.Text = "10";
            ProductoCodigoBarraEntry.Text = $"PROD-{Random.Shared.Next(100, 999)}";
            ModalProductoOverlay.IsVisible = true;
        }

        private void OnCerrarModalProductoClicked(object? sender, EventArgs e)
        {
            ModalProductoOverlay.IsVisible = false;
        }

        private async void OnGuardarProductoClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProductoNombreEntry.Text) ||
                !decimal.TryParse(ProductoPrecioEntry.Text, out decimal precio) || precio <= 0 ||
                !int.TryParse(ProductoStockEntry.Text, out int stock) || stock < 0)
            {
                await DisplayAlertAsync("Atención", "Ingrese un nombre, un precio válido mayor a 0 y un stock inicial válido.", "OK");
                return;
            }

            var nuevoProducto = new Producto
            {
                Nombre = ProductoNombreEntry.Text.Trim(),
                Categoria = string.IsNullOrWhiteSpace(ProductoCategoriaEntry.Text) ? "General" : ProductoCategoriaEntry.Text.Trim(),
                Precio = precio,
                Stock = stock,
                CodigoBarra = string.IsNullOrWhiteSpace(ProductoCodigoBarraEntry.Text) ? $"PROD-{Random.Shared.Next(100, 999)}" : ProductoCodigoBarraEntry.Text.Trim()
            };

            SetLoading(true);
            var productoCreado = await _apiService.CrearProductoAsync(nuevoProducto);
            SetLoading(false);

            if (productoCreado != null)
            {
                ModalProductoOverlay.IsVisible = false;
                await DisplayAlertAsync("Éxito 🎉", $"Producto '{productoCreado.Nombre}' registrado correctamente.", "OK");

                await CargarDatosAsync();

                var item = _productos.FirstOrDefault(p => p.Id == productoCreado.Id);
                if (item != null) ProductoPicker.SelectedItem = item;
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo guardar el producto.", "OK");
            }
        }

        // Interacción Tapped para la caja grande de selección
        private void OnClienteBoxTapped(object? sender, TappedEventArgs e)
        {
            ClientePicker.Focus();
        }

        private void OnProductoBoxTapped(object? sender, TappedEventArgs e)
        {
            ProductoPicker.Focus();
        }

        // Modal de Búsqueda de Cliente
        private void OnAbrirModalBuscarClienteClicked(object? sender, EventArgs e)
        {
            BuscarClienteEntry.Text = "";
            BuscarClienteCollectionView.ItemsSource = _clientes;
            ModalBuscarClienteOverlay.IsVisible = true;
        }

        private void OnCerrarModalBuscarClienteClicked(object? sender, EventArgs e)
        {
            ModalBuscarClienteOverlay.IsVisible = false;
        }

        private void OnBuscarClienteTextChanged(object? sender, TextChangedEventArgs e)
        {
            string query = e.NewTextValue?.Trim().ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                BuscarClienteCollectionView.ItemsSource = _clientes;
            }
            else
            {
                BuscarClienteCollectionView.ItemsSource = _clientes.Where(c =>
                    (c.Nombre?.ToLower().Contains(query) ?? false) ||
                    (c.DocumentoIdentidad?.ToLower().Contains(query) ?? false) ||
                    (c.Email?.ToLower().Contains(query) ?? false)
                ).ToList();
            }
        }

        private void OnSeleccionarClienteItemClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Cliente cliente)
            {
                ClientePicker.SelectedItem = cliente;
                ModalBuscarClienteOverlay.IsVisible = false;
            }
        }

        private void OnBuscarClienteSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Cliente cliente)
            {
                ClientePicker.SelectedItem = cliente;
                ModalBuscarClienteOverlay.IsVisible = false;
            }
        }

        // Modal de Búsqueda de Producto
        private void OnAbrirModalBuscarProductoClicked(object? sender, EventArgs e)
        {
            BuscarProductoEntry.Text = "";
            BuscarProductoCollectionView.ItemsSource = _productos;
            ModalBuscarProductoOverlay.IsVisible = true;
        }

        private void OnCerrarModalBuscarProductoClicked(object? sender, EventArgs e)
        {
            ModalBuscarProductoOverlay.IsVisible = false;
        }

        private void OnBuscarProductoTextChanged(object? sender, TextChangedEventArgs e)
        {
            string query = e.NewTextValue?.Trim().ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                BuscarProductoCollectionView.ItemsSource = _productos;
            }
            else
            {
                BuscarProductoCollectionView.ItemsSource = _productos.Where(p =>
                    (p.Nombre?.ToLower().Contains(query) ?? false) ||
                    (p.CodigoBarra?.ToLower().Contains(query) ?? false) ||
                    (p.Categoria?.ToLower().Contains(query) ?? false)
                ).ToList();
            }
        }

        private void OnSeleccionarProductoItemClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Producto producto)
            {
                ProductoPicker.SelectedItem = producto;
                OnProductoCardTapped(sender, new TappedEventArgs(producto));
                ModalBuscarProductoOverlay.IsVisible = false;
            }
        }

        private void OnBuscarProductoSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Producto producto)
            {
                ProductoPicker.SelectedItem = producto;
                OnProductoCardTapped(sender, new TappedEventArgs(producto));
                ModalBuscarProductoOverlay.IsVisible = false;
            }
        }

        // Modal de Configuración Servidor IP
        private void OnAbrirModalServerClicked(object? sender, EventArgs e)
        {
            ServerUrlEntry.Text = FacturacionApiService.GetConfiguredBaseUrl();
            ModalServerOverlay.IsVisible = true;
        }

        private void OnCerrarModalServerClicked(object? sender, EventArgs e)
        {
            ModalServerOverlay.IsVisible = false;
        }

        private async void OnGuardarServerClicked(object? sender, EventArgs e)
        {
            string url = ServerUrlEntry.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(url))
            {
                await DisplayAlertAsync("Atención", "Ingrese una URL o dirección IP válida.", "OK");
                return;
            }

            _apiService.UpdateBaseUrl(url);
            ModalServerOverlay.IsVisible = false;
            await CargarDatosAsync();
            await DisplayAlertAsync("Servidor Conectado 🌐", $"Dirección API actualizada a: {FacturacionApiService.GetConfiguredBaseUrl()}", "OK");
        }

        // ==========================================
        // MÓDULO 2: ESCÁNER DE CÓDIGO DE BARRAS
        // ==========================================
        private async void OnBarcodeScannedCompleted(object? sender, EventArgs e)
        {
            string code = BarcodeScannerEntry.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(code)) return;

            var producto = _productos.FirstOrDefault(p => 
                string.Equals(p.CodigoBarra, code, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Nombre, code, StringComparison.OrdinalIgnoreCase));

            if (producto == null)
            {
                await DisplayAlertAsync("Producto No Encontrado", $"No se encontró ningún producto registrado con el código '{code}'.", "OK");
                BarcodeScannerEntry.Text = "";
                return;
            }

            if (producto.Stock <= 0)
            {
                await DisplayAlertAsync("Sin Stock", $"El producto '{producto.Nombre}' no tiene stock disponible.", "OK");
                BarcodeScannerEntry.Text = "";
                return;
            }

            // Seleccionar producto en el UI
            ProductoPicker.SelectedItem = producto;

            // Agregar 1 unidad al carrito
            var itemExistente = _carrito.FirstOrDefault(c => c.ProductoId == producto.Id);
            if (itemExistente != null)
            {
                if (producto.Stock < itemExistente.Cantidad + 1)
                {
                    await DisplayAlertAsync("Stock Insuficiente", $"No hay más stock disponible para '{producto.Nombre}'.", "OK");
                    BarcodeScannerEntry.Text = "";
                    return;
                }
                itemExistente.Cantidad++;
            }
            else
            {
                _carrito.Add(new CarritoItemModel
                {
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = producto.Precio
                });
            }

            CarritoCollectionView.ItemsSource = null;
            CarritoCollectionView.ItemsSource = _carrito;
            ActualizarTotales();

            BarcodeScannerEntry.Text = "";
            BarcodeScannerEntry.Focus();
        }

        // ==========================================
        // MÓDULO 1: COBRO & CALCULADORA DE CAMBIO POS
        // ==========================================
        private decimal _totalActualCobro = 0;
        private CajaSesion? _cajaActiva;

        private void OnCerrarModalCobroClicked(object? sender, EventArgs e)
        {
            ModalCobroOverlay.IsVisible = false;
        }

        private void OnModalCobroFormaPagoChanged(object? sender, EventArgs e)
        {
            string formaPago = ModalCobroFormaPagoPicker.SelectedItem?.ToString() ?? "Efectivo";
            CalculadoraCambioStack.IsVisible = (formaPago == "Efectivo");
        }

        private void OnPagaConTextChanged(object? sender, TextChangedEventArgs e)
        {
            CalcularCambio();
        }

        private void OnBotonMontoExactoClicked(object? sender, EventArgs e)
        {
            ModalPagaConEntry.Text = _totalActualCobro.ToString("F0");
        }

        private void OnBotonMontoRapidoClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string montoStr && decimal.TryParse(montoStr, out decimal val))
            {
                ModalPagaConEntry.Text = val.ToString("F0");
            }
        }

        private void CalcularCambio()
        {
            decimal.TryParse(ModalPagaConEntry.Text, out decimal pagaCon);
            decimal cambio = pagaCon - _totalActualCobro;
            if (cambio < 0) cambio = 0;

            ModalCambioLabel.Text = $"${cambio:N2}";
        }

        private async void OnConfirmarCobroClicked(object? sender, EventArgs e)
        {
            if (ClientePicker.SelectedItem is not Cliente cliente)
            {
                await DisplayAlertAsync("Atención", "Debe seleccionar un cliente.", "OK");
                return;
            }

            if (!_carrito.Any())
            {
                await DisplayAlertAsync("Atención", "El carrito de compras está vacío.", "OK");
                return;
            }

            string formaPago = ModalCobroFormaPagoPicker.SelectedItem?.ToString() ?? "Efectivo";
            decimal.TryParse(ModalPagaConEntry.Text, out decimal pagaCon);
            decimal.TryParse(DescuentoEntry.Text, out decimal descuentoPct);

            if (formaPago == "Efectivo" && pagaCon < _totalActualCobro)
            {
                bool continuarSinCambio = await DisplayAlertAsync("Paga Con Menor al Total", $"El cliente paga con (${pagaCon:N2}) que es menor al total (${_totalActualCobro:N2}). ¿Desea procesar la venta de todos modos?", "Sí, Continuar", "Cancelar");
                if (!continuarSinCambio) return;
            }

            decimal cambio = pagaCon > _totalActualCobro ? pagaCon - _totalActualCobro : 0;

            var dto = new FacturaCreateDto
            {
                ClienteId = cliente.Id,
                UsuarioId = _usuarioLogueado?.Id,
                ImpuestoId = 1,
                FormaPago = formaPago,
                PagaCon = pagaCon,
                Cambio = cambio,
                CajaSesionId = _cajaActiva?.Id,
                DescuentoPorcentaje = descuentoPct,
                Notas = $"Cobro POS - Vendedor: {_usuarioLogueado?.Nombre ?? "Cajero"}",
                Detalles = _carrito.Select(c => new DetalleFacturaCreateDto
                {
                    ProductoId = c.ProductoId,
                    Cantidad = c.Cantidad
                }).ToList()
            };

            SetLoading(true);
            var factura = await _apiService.GenerarFacturaAsync(dto);
            SetLoading(false);

            if (factura != null)
            {
                _ultimaFactura = factura;
                ModalCobroOverlay.IsVisible = false;

                string opcion = await DisplayActionSheetAsync(
                    $"Venta Exitosa 🎉 | Factura #{factura.NumeroFactura}\nTotal: {factura.TotalCOPFormatted} | Cambio: $ {factura.Cambio:N0} COP",
                    "Cerrar",
                    null,
                    "📄 Abrir Factura Electrónica (PDF Formato Carta/A4)",
                    "🖨️ Imprimir Ticket POS (80mm)"
                );

                if (opcion == "📄 Abrir Factura Electrónica (PDF Formato Carta/A4)")
                {
                    await DescargarPdfFacturaElectronicaAsync(factura.Id);
                }
                else if (opcion == "🖨️ Imprimir Ticket POS (80mm)")
                {
                    await DescargarTicketPosAsync(factura.Id);
                }

                _carrito.Clear();
                DescuentoEntry.Text = "0";
                ActualizarTotales();
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error de Cobro", "No se pudo procesar la factura. Si es venta a crédito, verifique que el cliente no exceda su límite de crédito.", "OK");
            }
        }

        private async Task DescargarPdfFacturaElectronicaAsync(int facturaId)
        {
            SetLoading(true);
            try
            {
                byte[]? bytes = await _apiService.DescargarPdfBytesAsync(facturaId);
                if (bytes != null && bytes.Length > 0)
                {
                    string path = Path.Combine(FileSystem.CacheDirectory, $"FacturaElectronica_{facturaId}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);

                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(path)
                    });
                }
                else
                {
                    await DisplayAlertAsync("Error PDF", "No se pudo obtener el PDF de la factura electrónica.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error PDF", $"No se pudo abrir la Factura Electrónica: {ex.Message}", "OK");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task DescargarTicketPosAsync(int facturaId)
        {
            SetLoading(true);
            try
            {
                byte[]? bytes = await _apiService.DescargarTicketPosBytesAsync(facturaId);
                if (bytes != null && bytes.Length > 0)
                {
                    string path = Path.Combine(FileSystem.CacheDirectory, $"Ticket_{facturaId}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);

                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(path)
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error Ticket", $"No se pudo abrir el ticket: {ex.Message}", "OK");
            }
            finally
            {
                SetLoading(false);
            }
        }

        // ==========================================
        // MÓDULO 3: GESTIÓN DE CAJA & ARQUEO X / Z
        // ==========================================
        private async void OnAbrirModalCajaClicked(object? sender, EventArgs e)
        {
            if (_usuarioLogueado == null)
            {
                await DisplayAlertAsync("Sesión Requerida", "Debe iniciar sesión para administrar la caja.", "OK");
                return;
            }

            SetLoading(true);
            _cajaActiva = await _apiService.GetCajaActivaAsync(_usuarioLogueado.Id);
            SetLoading(false);

            if (_cajaActiva != null && _cajaActiva.Estado == "Abierta")
            {
                CajaAperturaStack.IsVisible = false;
                CajaAbiertaStack.IsVisible = true;

                CajaBaseLabel.Text = $"${_cajaActiva.MontoInicial:N2}";
                CajaVentasEfectivoLabel.Text = $"${_cajaActiva.TotalVentasEfectivo:N2}";
                CajaAbonosLabel.Text = $"${_cajaActiva.TotalAbonos:N2}";
                CajaMovimientosLabel.Text = $"${(_cajaActiva.TotalIngresos - _cajaActiva.TotalEgresos):N2}";
                
                decimal efectivoEsperado = _cajaActiva.MontoInicial + _cajaActiva.TotalVentasEfectivo + _cajaActiva.TotalAbonos + _cajaActiva.TotalIngresos - _cajaActiva.TotalEgresos;
                CajaEfectivoEsperadoLabel.Text = $"${efectivoEsperado:N2}";

                EfectivoRealEntry.Text = efectivoEsperado.ToString("F0");
            }
            else
            {
                CajaAperturaStack.IsVisible = true;
                CajaAbiertaStack.IsVisible = false;
            }

            ModalCajaOverlay.IsVisible = true;
        }

        private void OnCerrarModalCajaClicked(object? sender, EventArgs e)
        {
            ModalCajaOverlay.IsVisible = false;
        }

        private async void OnAbrirTurnoCajaClicked(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(MontoInicialEntry.Text, out decimal montoInicial) || montoInicial < 0)
            {
                await DisplayAlertAsync("Atención", "Ingrese un monto inicial válido mayor o igual a 0.", "OK");
                return;
            }

            var dto = new AperturaCajaDto
            {
                UsuarioId = _usuarioLogueado?.Id ?? 1,
                UsuarioNombre = _usuarioLogueado?.Nombre ?? "Cajero",
                MontoInicial = montoInicial
            };

            SetLoading(true);
            var caja = await _apiService.AbrirCajaAsync(dto);
            SetLoading(false);

            if (caja != null)
            {
                _cajaActiva = caja;
                ModalCajaOverlay.IsVisible = false;
                await DisplayAlertAsync("Turno Abierto 🔓", $"Turno de caja iniciado exitosamente con un fondo de base de ${montoInicial:N2}", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo abrir el turno de caja.", "OK");
            }
        }

        private async void OnCerrarTurnoCajaClicked(object? sender, EventArgs e)
        {
            if (_cajaActiva == null) return;

            if (!decimal.TryParse(EfectivoRealEntry.Text, out decimal efectivoReal) || efectivoReal < 0)
            {
                await DisplayAlertAsync("Atención", "Ingrese el efectivo real contado en caja.", "OK");
                return;
            }

            var dto = new CierreCajaDto
            {
                CajaSesionId = _cajaActiva.Id,
                EfectivoReal = efectivoReal,
                NotasCierre = $"Cierre Z realizado por {_usuarioLogueado?.Nombre}"
            };

            SetLoading(true);
            var cajaCerrada = await _apiService.CerrarCajaAsync(dto);
            SetLoading(false);

            if (cajaCerrada != null)
            {
                _cajaActiva = null;
                ModalCajaOverlay.IsVisible = false;

                string estadoDif = cajaCerrada.Diferencia == 0 ? " Exacto (Sin diferencias) ✅" : (cajaCerrada.Diferencia > 0 ? $" Sobrante (+${cajaCerrada.Diferencia:N2}) 📈" : $" Faltante (-${Math.Abs(cajaCerrada.Diferencia):N2}) ⚠️");

                await DisplayAlertAsync("Arqueo & Cierre de Caja Z 🔒",
                    $"Turno Cerrado Correctamente:\n\n" +
                    $"• Monto Inicial: ${cajaCerrada.MontoInicial:N2}\n" +
                    $"• Ventas Efectivo: ${cajaCerrada.TotalVentasEfectivo:N2}\n" +
                    $"• Abonos Recibidos: ${cajaCerrada.TotalAbonos:N2}\n" +
                    $"• Efectivo Esperado: ${cajaCerrada.EfectivoEsperado:N2}\n" +
                    $"• Efectivo Real Contado: ${cajaCerrada.EfectivoReal:N2}\n" +
                    $"• Resultado Arqueo:{estadoDif}", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error Cierre", "No se pudo realizar el cierre de caja.", "OK");
            }
        }

        // ==========================================
        // MÓDULO 4: CUENTAS POR COBRAR & ABONOS
        // ==========================================
        private async void OnAbrirModalCuentasClicked(object? sender, EventArgs e)
        {
            SetLoading(true);
            var cuentas = await _apiService.GetCuentasPorCobrarAsync();
            SetLoading(false);

            CuentasCollectionView.ItemsSource = cuentas;
            ModalCuentasOverlay.IsVisible = true;
        }

        private void OnCerrarModalCuentasClicked(object? sender, EventArgs e)
        {
            ModalCuentasOverlay.IsVisible = false;
        }

        private async void OnRegistrarAbonoClienteClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CuentaPorCobrarDto cuenta)
            {
                string montoStr = await DisplayPromptAsync("Registrar Abono 💵",
                    $"Cliente: {cuenta.ClienteNombre}\nSaldo Deuda Actual: ${cuenta.SaldoPendiente:N2}\n\nIngrese el monto del abono a ingresar:",
                    keyboard: Keyboard.Numeric);

                if (string.IsNullOrWhiteSpace(montoStr) || !decimal.TryParse(montoStr, out decimal montoAbono) || montoAbono <= 0)
                {
                    return;
                }

                if (montoAbono > cuenta.SaldoPendiente)
                {
                    bool aceptarExcedente = await DisplayAlertAsync("Monto Mayor a Deuda", $"El abono (${montoAbono:N2}) es mayor a la deuda total del cliente (${cuenta.SaldoPendiente:N2}). ¿Desea continuar?", "Sí", "Cancelar");
                    if (!aceptarExcedente) return;
                }

                var dto = new AbonoCreateDto
                {
                    ClienteId = cuenta.ClienteId,
                    CajaSesionId = _cajaActiva?.Id,
                    Monto = montoAbono,
                    FormaPago = "Efectivo",
                    Notas = $"Abono recibido por {_usuarioLogueado?.Nombre}",
                    UsuarioId = _usuarioLogueado?.Id,
                    UsuarioNombre = _usuarioLogueado?.Nombre
                };

                SetLoading(true);
                var abono = await _apiService.RegistrarAbonoAsync(dto);
                SetLoading(false);

                if (abono != null)
                {
                    await DisplayAlertAsync("Abono Registrado 🎉", $"Se abonaron ${montoAbono:N2} a la cuenta de {cuenta.ClienteNombre}.", "OK");
                    OnAbrirModalCuentasClicked(this, EventArgs.Empty);
                    await CargarDatosAsync();
                }
                else
                {
                    await DisplayAlertAsync("Error", "No se pudo registrar el abono.", "OK");
                }
            }
        }

        // ==========================================
        // MÓDULO 5: ADMINISTRACIÓN & AUDITORÍA COMPLETA
        // ==========================================
        private void AplicarFiltrosFacturas()
        {
            if (_facturas == null || AdminFacturasCollectionView == null) return;

            IEnumerable<Factura> resultado = _facturas;

            bool estaFiltrandoPorFecha = AdminFiltrarPorFechaCheckBox?.IsChecked ?? _filtrandoPorFecha;

            if (estaFiltrandoPorFecha && AdminFechaFiltroDatePicker != null && AdminFechaFiltroDatePicker.Date.HasValue)
            {
                DateTime fechaSeleccionada = AdminFechaFiltroDatePicker.Date.Value.Date;
                resultado = resultado.Where(f => f.Fecha.ToLocalTime().Date == fechaSeleccionada || f.Fecha.Date == fechaSeleccionada);
            }

            if (AdminBuscarFacturaEntry != null && !string.IsNullOrWhiteSpace(AdminBuscarFacturaEntry.Text))
            {
                string busqueda = AdminBuscarFacturaEntry.Text.Trim().ToLowerInvariant();
                resultado = resultado.Where(f =>
                    (f.NumeroFactura != null && f.NumeroFactura.ToLowerInvariant().Contains(busqueda)) ||
                    (f.ClienteNombreMostrar != null && f.ClienteNombreMostrar.ToLowerInvariant().Contains(busqueda)) ||
                    (f.Cliente != null && f.Cliente.Nombre != null && f.Cliente.Nombre.ToLowerInvariant().Contains(busqueda)) ||
                    (f.Estado != null && f.Estado.ToLowerInvariant().Contains(busqueda))
                );
            }

            AdminFacturasCollectionView.ItemsSource = null;
            AdminFacturasCollectionView.ItemsSource = resultado.ToList();
        }

        private void OnAdminFiltrarPorFechaCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            _filtrandoPorFecha = e.Value;
            AplicarFiltrosFacturas();
        }

        private void OnAdminFechaFiltroChanged(object? sender, DateChangedEventArgs e)
        {
            if (AdminFiltrarPorFechaCheckBox != null && AdminFiltrarPorFechaCheckBox.IsChecked)
            {
                _filtrandoPorFecha = true;
                AplicarFiltrosFacturas();
            }
        }

        private void OnAdminBuscarFacturaChanged(object? sender, TextChangedEventArgs e)
        {
            AplicarFiltrosFacturas();
        }

        private void OnMostrarTodasFacturasClicked(object? sender, EventArgs e)
        {
            _filtrandoPorFecha = false;
            if (AdminFiltrarPorFechaCheckBox != null) AdminFiltrarPorFechaCheckBox.IsChecked = false;
            if (AdminBuscarFacturaEntry != null) AdminBuscarFacturaEntry.Text = string.Empty;
            AplicarFiltrosFacturas();
        }

        private async void OnAnularFacturaAdminClicked(object? sender, EventArgs e)
        {
            var fact = (sender as Button)?.CommandParameter as Factura ?? (sender as BindableObject)?.BindingContext as Factura;
            if (fact == null) return;

            if (string.Equals(fact.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlertAsync("Factura Ya Anulada", $"La factura #{fact.NumeroFactura} ya se encuentra anulada.", "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync("Anular Factura 🚫",
                $"¿Está seguro de anular la factura #{fact.NumeroFactura} por valor de {fact.TotalCOPFormatted}?\n\nEsta acción devolverá los productos al stock del inventario y generará una nota de crédito.",
                "Sí, Anular Venta", "Cancelar");

            if (!confirmar) return;

            SetLoading(true);
            bool exito = await _apiService.AnularFacturaAsync(fact.Id, $"Anulada por {_usuarioLogueado?.Nombre ?? "Administrador"}");
            SetLoading(false);

            if (exito)
            {
                await DisplayAlertAsync("Anulación Exitosa 🎉", $"La factura #{fact.NumeroFactura} fue anulada y el stock de inventario fue devuelto.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error de Anulación", "No se pudo anular la factura en el servidor.", "OK");
            }
        }

        private async void OnDescargarPdfAdminClicked(object? sender, EventArgs e)
        {
            var fact = (sender as Button)?.CommandParameter as Factura ?? (sender as BindableObject)?.BindingContext as Factura;
            if (fact == null) return;

            await DescargarPdfFacturaElectronicaAsync(fact.Id);
        }

        private async void OnDescargarTicketAdminClicked(object? sender, EventArgs e)
        {
            var fact = (sender as Button)?.CommandParameter as Factura ?? (sender as BindableObject)?.BindingContext as Factura;
            if (fact == null) return;

            await DescargarTicketPosAsync(fact.Id);
        }

        private void OnEditarProductoAdminClicked(object? sender, EventArgs e)
        {
            var prod = (sender as Button)?.CommandParameter as Producto ?? (sender as BindableObject)?.BindingContext as Producto;
            if (prod == null) return;

            _productoEnEdicion = prod;
            EditProductoNombreEntry.Text = prod.Nombre;
            EditProductoCategoriaEntry.Text = prod.Categoria;
            EditProductoPrecioEntry.Text = prod.PrecioCOP.ToString("F0");
            EditProductoStockEntry.Text = prod.Stock.ToString();
            EditProductoCodigoBarraEntry.Text = prod.CodigoBarra;

            ModalEditarProductoOverlay.IsVisible = true;
        }

        private void OnCerrarModalEditProductoClicked(object? sender, EventArgs e)
        {
            ModalEditarProductoOverlay.IsVisible = false;
            _productoEnEdicion = null;
        }

        private async void OnGuardarEditProductoClicked(object? sender, EventArgs e)
        {
            if (_productoEnEdicion == null) return;

            if (string.IsNullOrWhiteSpace(EditProductoNombreEntry.Text))
            {
                await DisplayAlertAsync("Atención", "Ingrese el nombre del producto.", "OK");
                return;
            }

            if (!decimal.TryParse(EditProductoPrecioEntry.Text, out decimal precio) || precio <= 0)
            {
                await DisplayAlertAsync("Atención", "Ingrese un precio válido.", "OK");
                return;
            }

            if (!int.TryParse(EditProductoStockEntry.Text, out int stock) || stock < 0)
            {
                await DisplayAlertAsync("Atención", "Ingrese un stock válido.", "OK");
                return;
            }

            _productoEnEdicion.Nombre = EditProductoNombreEntry.Text.Trim();
            _productoEnEdicion.Categoria = EditProductoCategoriaEntry.Text?.Trim() ?? "";
            _productoEnEdicion.Precio = precio;
            _productoEnEdicion.Stock = stock;
            _productoEnEdicion.CodigoBarra = EditProductoCodigoBarraEntry.Text?.Trim() ?? "";

            SetLoading(true);
            bool res = await _apiService.ActualizarProductoAsync(_productoEnEdicion);
            SetLoading(false);

            if (res)
            {
                ModalEditarProductoOverlay.IsVisible = false;
                _productoEnEdicion = null;
                await DisplayAlertAsync("Producto Actualizado ✅", "Los datos del producto han sido modificados exitosamente.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo actualizar el producto en el servidor.", "OK");
            }
        }

        private async void OnEliminarProductoAdminClicked(object? sender, EventArgs e)
        {
            var prod = (sender as Button)?.CommandParameter as Producto ?? (sender as BindableObject)?.BindingContext as Producto;
            if (prod == null) return;

            bool confirmar = await DisplayAlertAsync("Eliminar Producto 🗑️", $"¿Está seguro de eliminar el producto '{prod.Nombre}'?", "Sí, Eliminar", "Cancelar");
            if (!confirmar) return;

            SetLoading(true);
            bool res = await _apiService.EliminarProductoAsync(prod.Id);
            SetLoading(false);

            if (res)
            {
                await DisplayAlertAsync("Producto Eliminado 🗑️", $"El producto '{prod.Nombre}' fue eliminado.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo eliminar el producto.", "OK");
            }
        }

        private void OnEditarClienteAdminClicked(object? sender, EventArgs e)
        {
            var cli = (sender as Button)?.CommandParameter as Cliente ?? (sender as BindableObject)?.BindingContext as Cliente;
            if (cli == null) return;

            _clienteEnEdicion = cli;
            EditClienteNombreEntry.Text = cli.Nombre;
            EditClienteDocumentoEntry.Text = cli.DocumentoIdentidad;
            EditClienteEmailEntry.Text = cli.Email;
            EditClienteTelefonoEntry.Text = cli.Telefono;
            EditClienteDireccionEntry.Text = cli.Direccion;

            ModalEditarClienteOverlay.IsVisible = true;
        }

        private void OnCerrarModalEditClienteClicked(object? sender, EventArgs e)
        {
            ModalEditarClienteOverlay.IsVisible = false;
            _clienteEnEdicion = null;
        }

        private async void OnGuardarEditClienteClicked(object? sender, EventArgs e)
        {
            if (_clienteEnEdicion == null) return;

            if (string.IsNullOrWhiteSpace(EditClienteNombreEntry.Text))
            {
                await DisplayAlertAsync("Atención", "Ingrese el nombre del cliente.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditClienteDocumentoEntry.Text))
            {
                await DisplayAlertAsync("Atención", "Ingrese el NIT / Documento del cliente.", "OK");
                return;
            }

            _clienteEnEdicion.Nombre = EditClienteNombreEntry.Text.Trim();
            _clienteEnEdicion.DocumentoIdentidad = EditClienteDocumentoEntry.Text.Trim();
            _clienteEnEdicion.Email = EditClienteEmailEntry.Text?.Trim() ?? "";
            _clienteEnEdicion.Telefono = EditClienteTelefonoEntry.Text?.Trim() ?? "";
            _clienteEnEdicion.Direccion = EditClienteDireccionEntry.Text?.Trim() ?? "";

            SetLoading(true);
            bool res = await _apiService.ActualizarClienteAsync(_clienteEnEdicion);
            SetLoading(false);

            if (res)
            {
                ModalEditarClienteOverlay.IsVisible = false;
                _clienteEnEdicion = null;
                await DisplayAlertAsync("Cliente Actualizado ✅", "Los datos del cliente han sido modificados exitosamente.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo actualizar el cliente en el servidor.", "OK");
            }
        }

        private async void OnEliminarClienteAdminClicked(object? sender, EventArgs e)
        {
            var cli = (sender as Button)?.CommandParameter as Cliente ?? (sender as BindableObject)?.BindingContext as Cliente;
            if (cli == null) return;

            bool confirmar = await DisplayAlertAsync("Eliminar Cliente 🗑️", $"¿Está seguro de eliminar al cliente '{cli.Nombre}'?", "Sí, Eliminar", "Cancelar");
            if (!confirmar) return;

            SetLoading(true);
            bool res = await _apiService.EliminarClienteAsync(cli.Id);
            SetLoading(false);

            if (res)
            {
                await DisplayAlertAsync("Cliente Eliminado 🗑️", $"El cliente '{cli.Nombre}' fue eliminado.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo eliminar el cliente.", "OK");
            }
        }

        // ==========================================
        // MÓDULO 6: GESTIÓN DE CAJEROS Y USUARIOS
        // ==========================================
        private void OnAbrirModalUsuarioClicked(object? sender, EventArgs e)
        {
            NuevoUsuarioNombreEntry.Text = string.Empty;
            NuevoUsuarioUsernameEntry.Text = string.Empty;
            NuevoUsuarioPasswordEntry.Text = "123";
            NuevoUsuarioRolPicker.SelectedIndex = 0; // Cajero
            ModalUsuarioOverlay.IsVisible = true;
        }

        private void OnCerrarModalUsuarioClicked(object? sender, EventArgs e)
        {
            ModalUsuarioOverlay.IsVisible = false;
        }

        private async void OnGuardarNuevoUsuarioClicked(object? sender, EventArgs e)
        {
            string nombre = NuevoUsuarioNombreEntry.Text?.Trim() ?? "";
            string username = NuevoUsuarioUsernameEntry.Text?.Trim() ?? "";
            string password = NuevoUsuarioPasswordEntry.Text ?? "123";
            string rol = NuevoUsuarioRolPicker.SelectedItem?.ToString() ?? "Cajero";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                await DisplayAlertAsync("Atención", "Debe ingresar el nombre completo del cajero/usuario.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlertAsync("Atención", "Debe ingresar el nombre de usuario (Username).", "OK");
                return;
            }

            var usuarioNuevo = new Usuario
            {
                Nombre = nombre,
                Username = username,
                PasswordHash = password,
                Rol = rol,
                Activo = true
            };

            SetLoading(true);
            var usuarioCreado = await _apiService.CrearUsuarioAsync(usuarioNuevo);
            SetLoading(false);

            if (usuarioCreado != null)
            {
                ModalUsuarioOverlay.IsVisible = false;
                await DisplayAlertAsync("Cajero Creado 🎉", $"El usuario '{usuarioCreado.Username}' ({usuarioCreado.Nombre}) con rol '{usuarioCreado.Rol}' fue registrado exitosamente.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo crear el usuario. Verifique si el nombre de usuario ya está registrado.", "OK");
            }
        }

        private async void OnEliminarUsuarioAdminClicked(object? sender, EventArgs e)
        {
            var usr = (sender as Button)?.CommandParameter as Usuario ?? (sender as BindableObject)?.BindingContext as Usuario;
            if (usr == null) return;

            if (_usuarioLogueado != null && _usuarioLogueado.Id == usr.Id)
            {
                await DisplayAlertAsync("Acción no permitida", "No puede eliminar su propio usuario activo en uso.", "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync("Eliminar Usuario 🗑️", $"¿Está seguro de eliminar al cajero/usuario '{usr.Nombre}' ({usr.Username})?", "Sí, Eliminar", "Cancelar");
            if (!confirmar) return;

            SetLoading(true);
            bool res = await _apiService.EliminarUsuarioAsync(usr.Id);
            SetLoading(false);

            if (res)
            {
                await DisplayAlertAsync("Usuario Eliminado 🗑️", $"El usuario '{usr.Username}' fue eliminado.", "OK");
                await CargarDatosAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo eliminar el usuario.", "OK");
            }
        }
    }
}
