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
    accessToken:string;
    expiredInSeconds:number;
    user:AuthenticatedUser;
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
      return JSON.parse(value) as AuthenticatedUser;
    } catch {
      logout();
      return null;
    }
  }
  
  export function isAuthenticated(): boolean {
    return getAccessToken() !== null;
  }