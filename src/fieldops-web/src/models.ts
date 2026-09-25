export type  WorkOrderPriority =  | "Low"| "Medium"  | "High"  | "Emergency";

export type WorkOrderStatus =   | "Submitted"  | "Assigned"  | "InProgress"  | "WaitingForParts"  | "Completed"  | "Cancelled";

export interface  WorkOrder {
  id: string;
  title: string;
  description?: string;
  location: string;
  priority: WorkOrderPriority;
  status: WorkOrderStatus;
  assignedTechnicianId?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface Technician {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  skills?: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
}
export interface CreateWorkOrderRequest{
  title:string;
  description? :string;
  location: string;
  priority: WorkOrderPriority;
}
export interface AssignWorkOrderRequest{
  technicianId:string;
}
export interface CreateTechnicianRequest{
  firstName:string;
  lastName:string;
  email:string;
  phoneNumber?:string;
  skills?:string;
}

export interface UpdateTechnicianRequest extends CreateTechnicianRequest{
  isActive:boolean;
}

export interface PagedWorkOrdersResponse {
  items: WorkOrder[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  openCount: number;
  emergencyCount: number;
  allCount:number;
}

export interface UpdateWorkOrderStatusRequest{
  status:WorkOrderStatus;
}