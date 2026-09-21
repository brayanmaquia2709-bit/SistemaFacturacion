import express, { Application, Request, Response, NextFunction } from 'express';
import helmet from 'helmet';
import rateLimit from 'express-rate-limit';
import cors from 'cors';
import { authenticateJWT, authorizeRoles } from './middlewares/authMiddleware';
import { InvoiceController } from './controllers/invoiceController';
import { InvoiceRepository } from './repositories/invoiceRepository';

const app: Application = express();
const PORT = process.env.PORT || 4000;

// 1. REQUERIMIENTO 2: Cabeceras de Seguridad con Helmet (CSP, HSTS, X-Frame-Options, XSS Filter)
app.use(
  helmet({
    contentSecurityPolicy: {
      directives: {
        defaultSrc: ["'self'"],
        scriptSrc: ["'self'"],
        styleSrc: ["'self'", "'unsafe-inline'"],
        imgSrc: ["'self'", 'data:'],
      },
    },
    crossOriginEmbedderPolicy: true,
  })
);

// 2. Configuración CORS Restringida
app.use(
  cors({
    origin: process.env.CLIENT_ORIGIN || 'http://localhost:3000',
    credentials: true,
    methods: ['GET', 'POST', 'PUT', 'DELETE'],
    allowedHeaders: ['Content-Type', 'Authorization', 'X-Requested-With'],
  })
);

// 3. Control de Mitigación de Fuerza Bruta y DoS (Rate Limiting)
const apiLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutos
  max: 100, // Máximo 100 peticiones por IP
  message: { error: 'Demasiadas solicitudes desde esta IP, intente más tarde.' },
});
app.use('/api/', apiLimiter);

// Parseo seguro de JSON con límite de tamaño para prevenir Payloads masivos
app.use(express.json({ limit: '10kb' }));

// Inicialización de Dependencias (Inyección de Dependencias)
const mockDbPool = {}; // Reemplazar con el pool de base de datos real (pg.Pool / Knex / Prisma)
const invoiceRepository = new InvoiceRepository(mockDbPool);
const invoiceController = new InvoiceController(invoiceRepository);

// Health check
app.get('/health', (req: Request, res: Response) => {
  res.status(200).json({ status: 'UP', timestamp: new Date() });
});

// 4. RUTAS SECURE DE LA API DE FACTURACIÓN

// Emisión de Facturas: Requiere ser usuario Autenticado con Rol ADMIN o EMPLOYEE
app.post(
  '/api/v1/invoices',
  authenticateJWT,
  authorizeRoles('ADMIN', 'EMPLOYEE'),
  invoiceController.createInvoice
);

// Operación Crítica: Anulación de Facturas - Requiere estrictamente Rol ADMIN (RBAC)
app.put(
  '/api/v1/invoices/:id/cancel',
  authenticateJWT,
  authorizeRoles('ADMIN'),
  (req: Request, res: Response) => {
    res.status(200).json({ message: `Factura ${req.params.id} anulada por el administrador.` });
  }
);

// Manejador Global de Errores
app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
  console.error('Error no controlado:', err.stack);
  res.status(500).json({ error: 'Error del Servidor', message: 'Ocurrió un fallo inesperado en la API.' });
});

if (process.env.NODE_ENV !== 'test') {
  app.listen(PORT, () => {
    console.log(`🔒 Server de Facturación Empresarial corriendo en el puerto ${PORT}`);
  });
}

export default app;
