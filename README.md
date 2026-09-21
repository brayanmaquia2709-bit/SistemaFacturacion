# Sistema de Facturación Empresarial Seguro y Responsivo

Este repositorio contiene la arquitectura de referencia, estructura de archivos y módulos clave para un **Sistema de Facturación de Nivel Empresarial**, diseñado con enfoque en alta disponibilidad, responsividad fluida (UI/UX) y ciberseguridad avanzada.

---

## 🏛️ Arquitectura del Sistema

```
SistemaFacturacion/
├── client/                      # Frontend (React + TypeScript + Tailwind CSS)
│   └── src/
│       ├── components/          # Vista Principal de Facturación y Tabla Adaptable
│       ├── types/               # Tipados TypeScript estrictos para Facturación
│       └── utils/               # Sanitización contra ataques XSS
│
└── server/                      # Backend (Node.js + Express + TypeScript + ORM/Prepared Statements)
    └── src/
        ├── config/              # Cabeceras Helmet, CSRF, Rate Limiting
        ├── middlewares/         # Autenticación JWT y Control de Acceso por Roles (RBAC)
        ├── schemas/             # Validación estricta con Zod
        ├── services/            # Cifrado Bcrypt y lógica de autenticación
        ├── repositories/        # Consultas SQL Parametrizadas (Prevención SQL Injection)
        └── controllers/         # Endpoints seguros de emisión y consulta de facturas
```

---

## 🛡️ Medidas de Ciberseguridad Implementadas

1. **Autenticación JWT con Expiración Corta y Refresh Token**: Manejo seguro de credenciales con control de tiempo de vida.
2. **Control de Acceso Basado en Roles (RBAC)**: Diferenciación estricta entre `ADMIN` y `EMPLOYEE`.
3. **Prevención de Inyección SQL**: Consultas parametrizadas con sintaxis de marcadores de posición (`$1, $2, ...`) evitando concatenación de strings.
4. **Protección XSS & Sanidad de Datos**: Sanitización estricta de entradas de texto libre mediante `DOMPurify` / `sanitize-html` tanto en cliente como en servidor, además de escapado automático de React y cabeceras CSP.
5. **Protección CSRF & Cabeceras de Seguridad**: Configuración de `Helmet`, `SameSite=Strict` en cookies y comprobación de cabecera `Origin`/`X-Requested-With`.
6. **Validación Estricta de Negocio (Zod)**: Recálculo redundante del IVA y totales en el Backend para evitar alteraciones o manipulaciones en las peticiones HTTP del navegador.
7. **Cifrado de Contraseñas**: Uso de `bcrypt` con costo de sal (salt rounds = 12).

---

## 🎨 UI/UX Responsivo y Proporcionado

- **CSS Grid Flexbox Dinámico**: Adaptación fluida de 1 columna en teléfonos móviles a 3 columnas en monitores de alta resolución.
- **Tablas Adaptables**: Envoltorio horizontal con `overflow-x-auto` e hiper-legibilidad en pantallas táctiles sin desplazar el marco general de la web.
