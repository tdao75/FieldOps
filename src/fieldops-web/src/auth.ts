const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

const TOKEN_KEY = "fieldops_accss_token";
const USER_KEY = "fieldops_user";

export interface LoginRequest{
    email:string;
    password:string;
}

export interface AuthenticatedUser{
    id:string;
    email:string;
    firstName:string;
    lastName:string;
}

export interface LoginResponse{
    accessToken: string;
    expiresInSeconds: number;
    roles: string[];
    user: AuthenticatedUser;
}

export interface AuthenticatedUser {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
 }

export interface RegisterRequest {
    email:string;
    password:string;
    firstName:string;
    lastName:string;
}

export async function login(request:LoginRequest):Promise<LoginResponse> {
    const response = await fetch(`${API_BASE_URL}/api/auth/login`,{
        method:"POST",
        headers:{
            "Content-Type":"application/json",
        },
        body:JSON.stringify(request),
    });

    if(!response.ok){
        if(response.status === 401){
            throw new Error("Invalid email or password.");
        }
        throw new Error(`Login failed: ${response.status}`);
    }

    const result = (await response.json()) as LoginResponse;

    result.user.roles = result.roles;

    localStorage.setItem(TOKEN_KEY, result.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(result.user));

    return result;
}

export function logout():void{
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}

export function getAccessToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
}

export function getCurrentUser(): AuthenticatedUser | null {
    const value = localStorage.getItem(USER_KEY);
  
    if (!value) {
      return null;
    }
  
    try {
      const user = JSON.parse(value) as AuthenticatedUser;
  
      return {
        ...user,
        roles: Array.isArray(user.roles) ? user.roles: [],
      };
    } catch {
      logout();
      return null;
    }
  }
  export function isAuthenticated(): boolean {
    return getAccessToken() !== null;
  }

  export async function register(request:RegisterRequest,):Promise<LoginResponse> {
    const response = await fetch(`${API_BASE_URL}/api/auth/register`,{
        method:"POST",
        headers:{
            Accept:"application/json",
            "Content-Type":"application/json"
        },
        body:JSON.stringify(request),
    },);

    if(!response.ok){
        if(response.status ===409){
            throw new Error("An account with this email already exists.",);
        }
    }
    const errorBody = await response.text();
    throw new Error(errorBody || `Registration failed: ${response.status}`, );

    
    // Registration does not return a JWT, so sign in
    // immediately after creating the account.
    return login({
        email:request.email,
        password:request.password,
    });
  }