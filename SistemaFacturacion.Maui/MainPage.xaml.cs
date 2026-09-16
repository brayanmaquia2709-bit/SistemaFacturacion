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

        public string UnitPriceText => $"${PrecioUnitario:N2} c/u";
        public string CantidadText => $"x{Cantidad}";
        public string SubtotalText => $"${Subtotal:N2}";
    }

    public partial class MainPage : ContentPage
    {
        private readonly FacturacionApiService _apiService;
        private readonly ObservableCollection<CarritoItemModel> _carrito = new();
        private List<Cliente> _clientes = new();
        private List<Producto> _productos = new();
        private List<Usuario> _usuarios = new();
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

                    // Actualizar catálogo del Picker manteniendo la posición si corresponde
                    ProductoPicker.ItemsSource = null;
                    ProductoPicker.ItemsSource = _productos;

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
                UserSessionLabel.Text = $"👤 Vendedor: {_usuarioLogueado!.Nombre} ({_usuarioLogueado.Rol})";
                UserSessionLabel.TextColor = Color.FromArgb("#FDE047");
            }
            else
            {
                UserSessionLabel.Text = "🔒 Sesión Bloqueada: Inicie Sesión para Operar";
                UserSessionLabel.TextColor = Color.FromArgb("#F87171");
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (MainLayoutGrid == null || LeftColumnStack == null || RightColumnStack == null) return;

            if (width < 768) // Modo Celular (Teléfono Móvil en Vertical: 1 sola columna fluida)
            {
                MainLayoutGrid.ColumnDefinitions.Clear();
                MainLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                MainLayoutGrid.RowDefinitions.Clear();
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(LeftColumnStack, 0);
                Grid.SetRow(LeftColumnStack, 0);

                Grid.SetColumn(RightColumnStack, 0);
                Grid.SetRow(RightColumnStack, 1);
            }
            else // Modo Tablet / PC (2 columnas lado a lado)
            {
                MainLayoutGrid.ColumnDefinitions.Clear();
                MainLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                MainLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                MainLayoutGrid.RowDefinitions.Clear();
                MainLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(LeftColumnStack, 0);
                Grid.SetRow(LeftColumnStack, 0);

                Grid.SetColumn(RightColumnStack, 1);
                Grid.SetRow(RightColumnStack, 0);
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

                ClientePicker.ItemsSource = _clientes;
                ProductoPicker.ItemsSource = _productos;
                UsuariosRapidosPicker.ItemsSource = _usuarios;

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
                ClienteCardBadge.IsVisible = true;
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
                ProductoBadgePrecio.Text = $"💰 Precio: ${producto.Precio:N2}";
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

                ProductoCardBadge.IsVisible = true;
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
            decimal impuestos = subtotalNeto * 0.19m; // 19% IVA
            decimal total = subtotalNeto + impuestos;

            SubtotalLabel.Text = $"${subtotalBruto:N2}";
            DescuentoLabel.Text = $"-${descuentoMonto:N2} ({pctDescuento}%)";
            ImpuestoLabel.Text = $"${impuestos:N2}";
            TotalLabel.Text = $"${total:N2}";
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
                LoginPasswordEntry.Text = user.PasswordHash;
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
                ActualizarEstadoAcceso();
                ModalLoginOverlay.IsVisible = false;
                await DisplayAlertAsync("Bienvenido 🎉", $"Sesión iniciada como {usuario.Nombre} [{usuario.Rol}]", "OK");
            }
            else
            {
                string currentUrl = FacturacionApiService.GetConfiguredBaseUrl();
                bool config = await DisplayAlertAsync("Error de Conexión / Autenticación",
                    $"No se pudo conectar a la API en: {currentUrl}\n\nSi estás usando tu celular físico, asegúrate de conectarte a la red Wi-Fi e ingresar la IP de tu PC (192.168.6.157).\n\n¿Deseas configurar la IP del Servidor ahora?",
                    "Configurar IP 🌐", "Reintentar");

                if (config)
                {
                    OnAbrirModalServerClicked(this, EventArgs.Empty);
                }
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
                ModalBuscarProductoOverlay.IsVisible = false;
            }
        }

        private void OnBuscarProductoSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Producto producto)
            {
                ProductoPicker.SelectedItem = producto;
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

                PdfBtn.IsEnabled = true;
                CorreoBtn.IsEnabled = true;
                WhatsAppBtn.IsEnabled = true;

                string msg = formaPago == "Credito"
                    ? $"Venta a Crédito registrada para '{cliente.Nombre}'. Nuevo saldo pendiente: ${cliente.SaldoPendiente + factura.Total:N2}"
                    : $"Venta Exitosa 🎉\nFactura #{factura.NumeroFactura}\nTotal: ${factura.Total:N2}\nCambio a Entregar: ${factura.Cambio:N2}";

                bool verTicket = await DisplayAlertAsync("Cobro Exitoso 🚀", msg + "\n\n¿Desea abrir/descargar el Ticket de Caja POS (58mm/80mm)?", "Imprimir/Abrir Ticket 🖨️", "Cerrar");

                if (verTicket)
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
    }
}
