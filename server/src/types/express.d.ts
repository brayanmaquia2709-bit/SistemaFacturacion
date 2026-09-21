import { UserRole } from '../../client/src/types/billing';

declare global {
  namespace Express {
    interface Request {
      user?: {
        userId: string;
        email: string;
        role: 'ADMIN' | 'EMPLOYEE';
      };
    }
  }
}
