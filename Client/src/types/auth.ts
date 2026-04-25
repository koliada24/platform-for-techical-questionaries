export type UserRole = 'Student' | 'Teacher';

export interface User {
  email: string;
  fullName: string | null;
  role: UserRole;
  hasGoogleLink: boolean;
  pictureUrl: string | null;
}
