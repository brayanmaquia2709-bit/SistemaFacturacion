import { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';

const JWT_SECRET = process.env.JWT_SECRET || 'SUPER_SECRET_ENTERPRISE_KEY_CHANGE_IN_PROD';

export interface TokenPayload {
  userId: string;
  email: string;
  role: 'ADMIN' | 'EMPLOYEE';
}

/**
 * REQUERIMIENTO 2: Middleware de Autenticación JWT con Expiración
 */
export const authenticateJWT = (req: Request, res: Response, next: NextFunction) => {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({
      error: 'Acceso Denegado',
      message: 'Token de autenticación no proporcionado o formato inválido.',
    });
  }

  const token = authHeader.split(' ')[1];

  try {
    const decoded = jwt.verify(token, JWT_SECRET) as TokenPayload;
    req.user = decoded;
    next();
  } catch (error) {
    if (error instanceof jwt.TokenExpiredError) {
      return res.status(401).json({
        error: 'Token Expirado',
        message: 'La sesión ha expirado. Por favor inicie sesión nuevamente.',
      });
    }

    return res.status(403).json({
      error: 'Token Inválido',
      message: 'Firma de token no autorizada o alterada.',
    });
  }
};

/**
 * REQUERIMIENTO 2: Control de Acceso Basado en Roles (RBAC)
 */
export const authorizeRoles = (...allowedRoles: Array<'ADMIN' | 'EMPLOYEE'>) => {
  return (req: Request, res: Response, next: NextFunction) => {
    if (!req.user) {
      return res.status(401).json({ error: 'No autenticado' });
    }

    if (!allowedRoles.includes(req.user.role)) {
      return res.status(403).json({
        error: 'Acceso Restringido',
        message: `El rol '${req.user.role}' no tiene permisos para realizar esta operación.`,
      });
    }

    next();
  };
};
