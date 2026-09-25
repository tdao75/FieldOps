import { getAccessToken, logout } from "./auth";
import type {
  CreateTechnicianRequest,
  UpdateTechnicianRequest,
  AssignWorkOrderRequest,
  CreateWorkOrderRequest,
  PagedWorkOrdersResponse,
  Technician,
  WorkOrder,
  UpdateWorkOrderStatusRequest,
  WorkOrderStatus,
  WorkOrderPriority,
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

async function deleteRequest(path:string):Promise<void> {
  const response = await apiFetch(path,{method:"DELETE",headers:{Accept:"application/json",},});
  if(!response.ok){
    const errorBody = await response.text();
    throw new Error(errorBody ||`Request failed: ${response.status} ${response.statusText}`,);
  }
}

export function getWorkOrders(
  pageNumber = 1,
  pageSize = 10,
  search = "",
  status: WorkOrderStatus | "" = "",
  priority: WorkOrderPriority | "" = "",
): Promise<PagedWorkOrdersResponse> {
  const parameters = new URLSearchParams({
    pageNumber: pageNumber.toString(),
    pageSize: pageSize.toString(),
  });

  if (search.trim()) { parameters.set("search", search.trim());}

  if (status) { parameters.set("status", status); }

  if (priority) { parameters.set("priority", priority);}

  return getJson<PagedWorkOrdersResponse>(`/api/workorders?${parameters.toString()}`,);
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

export function updateTechnician(technicianId:string,request:UpdateTechnicianRequest,):Promise<Technician>{
  return putJson<Technician, UpdateTechnicianRequest>(`/api/technicians/${technicianId}`, request,);
}

export function deactivateTechnician(technicianId:string,):Promise<void>{
  return deleteRequest(`/api/technicians/${technicianId}`,);
}

export function updateWorkOrderStatus(workOrderId:string,status:WorkOrderStatus,):Promise<WorkOrder>{
  const request:UpdateWorkOrderStatusRequest ={status,};
  return putJson<WorkOrder,UpdateWorkOrderStatusRequest>(`/api/workorders/${workOrderId}/status`, request,);
}