import bcrypt from 'bcrypt';
import jwt from 'jsonwebtoken';
import { TokenPayload } from '../middlewares/authMiddleware';

const SALT_ROUNDS = 12; // Cifrado fuerte bcrypt
const JWT_SECRET = process.env.JWT_SECRET || 'SUPER_SECRET_ENTERPRISE_KEY_CHANGE_IN_PROD';
const JWT_EXPIRATION = '15m'; // Token de corta duración

export class AuthService {
  /**
   * Cifra la contraseña del usuario antes de guardarla en la BD
   */
  static async hashPassword(password: string): Promise<string> {
    return await bcrypt.hash(password, SALT_ROUNDS);
  }

  /**
   * Compara la contraseña en texto plano contra el hash bcrypt guardado
   */
  static async verifyPassword(plainPassword: string, passwordHash: string): Promise<boolean> {
    return await bcrypt.compare(plainPassword, passwordHash);
  }

  /**
   * Genera un Token JWT firmado con payload de rol y usuario
   */
  static generateToken(payload: TokenPayload): string {
    return jwt.sign(payload, JWT_SECRET, {
      expiresIn: JWT_EXPIRATION,
      algorithm: 'HS256',
    });
  }
}
