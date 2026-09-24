import { getAccessToken, logout } from "./auth";
import type {
  CreateTechnicianRequest,
  AssignWorkOrderRequest,
  CreateWorkOrderRequest,
  Technician,
  WorkOrder,
} from "./models";

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

function buildApiUrl(path: string): string {
  const normalizedBaseUrl = apiBaseUrl.replace(/\/+$/, "");

  const normalizedPath = path.replace(/^\/+/, "");

  return `${normalizedBaseUrl}/${normalizedPath}`;
}

async function apiFetch(
  path: string,
  options: RequestInit = {},
): Promise<Response> {
  const headers = new Headers(options.headers);
  const accessToken = getAccessToken();

  if (accessToken) {
    headers.set(
      "Authorization",
      `Bearer ${accessToken}`,
    );
  }

  const response = await fetch(buildApiUrl(path), {
    ...options,
    headers,
  });

  if (response.status === 401) {
    logout();
    window.dispatchEvent(new Event("fieldops:unauthorized"),);
    throw new Error("Your session expired. Please sign in again.",);
  }

  return response;
}

async function getJson<T>(path: string): Promise<T> {
  const response = await apiFetch(path);

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`,);
  }

  return response.json() as Promise<T>;
}

async function postJson<TResponse, TRequest>(path: string,request: TRequest,): Promise<TResponse> {
  const response = await apiFetch(path, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorBody = await response.text();

    throw new Error(errorBody || `Request failed: ${response.status} ${response.statusText}`,
    );
  }

  return response.json() as Promise<TResponse>;
}

async function putJson<TResponse, TRequest>(path: string, request: TRequest,): Promise<TResponse> {
  const response = await apiFetch(path, {
    method: "PUT",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorBody = await response.text();

    throw new Error(errorBody || `Request failed: ${response.status} ${response.statusText}`,);
  }

  return response.json() as Promise<TResponse>;
}

export function getWorkOrders(): Promise<WorkOrder[]> {
  return getJson<WorkOrder[]>("/api/workorders");
}

export function getTechnicians(): Promise<Technician[]> {
  return getJson<Technician[]>("/api/technicians");
}

export function createWorkOrder(request: CreateWorkOrderRequest,): Promise<WorkOrder> {
  return postJson<WorkOrder, CreateWorkOrderRequest>("/api/workorders",request,);
}

export function assignWorkOrder(workOrderId: string, technicianId: string,): Promise<WorkOrder> {
  const request: AssignWorkOrderRequest = {
    technicianId,
  };

  return putJson<WorkOrder, AssignWorkOrderRequest>(`/api/workorders/${workOrderId}/assignment`, request,);
}

export function createTechnician(request: CreateTechnicianRequest,): Promise<Technician> {
  return postJson<Technician, CreateTechnicianRequest>("/api/technicians", request,);
}