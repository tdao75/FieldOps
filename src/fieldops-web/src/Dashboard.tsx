import { useCallback, useEffect, useState } from "react";
import { getTechnicians, getWorkOrders } from "./api";
import type { Technician, WorkOrder } from "./models";
import CreateWorkOrderForm from "./CreateWorkOrderForm";
import AssignmentControl from "./AssignmentControl";
import TechniciansPanel from "./TechniciansPanel";
import WorkOrderStatusControl from "./WorkOrderStatusControl";
import "./App.css";

interface DashboardProps {
  canManage: boolean;
}

function Dashboard({ canManage }: DashboardProps) {
  const [workOrders, setWorkOrders] = useState<WorkOrder[]>([]);
  const [technicians, setTechnicians] = useState<Technician[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [openCount, setOpenCount] = useState(0);
  const [emergencyCount, setEmergencyCount] = useState(0);
  const pageSize = 10;

  const loadDashboard = useCallback(async () => {
    try {
      //setIsLoading(true);
      setError("");

      const [workOrderData, technicianData] = await Promise.all([
        getWorkOrders(pageNumber, pageSize),
        getTechnicians(),
      ]);

      setWorkOrders(workOrderData.items);
      setTotalCount(workOrderData.totalCount);
      setTotalPages(workOrderData.totalPages);
      setOpenCount(workOrderData.openCount);
      setEmergencyCount(workOrderData.emergencyCount);
      setTechnicians(technicianData);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to load FieldOps data.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber]);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  if (isLoading) {
    return <main className="page-message">Loading FieldOps…</main>;
  }

  return (
    <main className="app-shell">
      <header className="page-header">
        <div>
          <p className="eyebrow">Operations dashboard</p>
          <h1>FieldOps</h1>
          <p className="subtitle">Work orders and field technician activity</p>
        </div>

        <div className="service-status">
          <span className="status-dot" />
          API gateway connected
        </div>
      </header>

      {error && <div className="error-message">{error}</div>}

      <section className="summary-grid">
        <article className="summary-card">
          <span>Total work orders</span>
          <strong>{totalCount}</strong>
        </article>

        <article className="summary-card">
          <span>Open work orders</span>
          <strong>{openCount}</strong>
        </article>

        <article className="summary-card urgent">
          <span>Emergencies</span>
          <strong>{emergencyCount}</strong>
        </article>

        <article className="summary-card">
          <span>Active technicians</span>
          <strong>{technicians.length}</strong>
        </article>
      </section>

      {canManage ? (
        <>
          <CreateWorkOrderForm onCreated={loadDashboard} />
          <TechniciansPanel
            technicians={technicians}
            onCreated={loadDashboard}
          />
        </>
      ) : (
        <div className="access-note">
          Technician access is read-only. Dispatchers manage work orders and
          assignments.
        </div>
      )}

      <section className="content-panel">
        <div className="panel-heading">
          <div>
            <p className="eyebrow">Current workload</p>
            <h2>Work orders</h2>
          </div>
        </div>

        {workOrders.length === 0 ? (
          <p className="empty-message">No work orders found.</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Location</th>
                  <th>Priority</th>
                  <th>Status</th>
                  <th>Technician</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {workOrders.map((workOrder) => (
                  <tr key={workOrder.id}>
                    <td className="title-cell">{workOrder.title}</td>
                    <td>{workOrder.location}</td>
                    <td>
                      <span
                        className={`badge priority-${workOrder.priority.toLowerCase()}`}
                      >
                        {workOrder.priority}
                      </span>
                    </td>
                    <td>
                      <WorkOrderStatusControl
                        workOrder={workOrder}
                        onUpdated={loadDashboard}
                      />
                    </td>
                    <td>
                      {canManage ? (
                        <AssignmentControl
                          workOrder={workOrder}
                          technicians={technicians}
                          onAssigned={loadDashboard}
                        />
                      ) : (
                        <span className="read-only-value">
                          {workOrder.assignedTechnicianId
                            ? "Assigned"
                            : "Unassigned"}
                        </span>
                      )}
                    </td>
                    <td>
                      {new Date(workOrder.createdAtUtc).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {totalCount > 0 && (
          <div className="pagination">
            <button
              type="button"
              disabled={pageNumber <= 1}
              onClick={() =>
                setPageNumber((current) => Math.max(1, current - 1))
              }
            >
              Previous
            </button>

            <span>
              Page {pageNumber} of {Math.max(totalPages, 1)}
            </span>

            <button
              type="button"
              disabled={totalPages === 0 || pageNumber >= totalPages}
              onClick={() =>
                setPageNumber((current) => Math.min(totalPages, current + 1))
              }
            >
              Next
            </button>
          </div>
        )}
      </section>
    </main>
  );
}

export default Dashboard;
