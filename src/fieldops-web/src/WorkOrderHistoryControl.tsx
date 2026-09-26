import { useState } from "react";
import { getWorkOrderHistory } from "./api";
import type { WorkOrder, WorkOrderAuditEntry } from "./models";

interface WorkOrderHistoryControlProps {
  workOrder: WorkOrder;
}

function formatAction(entry: WorkOrderAuditEntry): string {
  switch (entry.action) {
    case "Created":
      return "Work order created";

    case "DetailsUpdated":
      return "Work order details updated";

    case "Assigned":
      return "Technician assigned";

    case "Reassigned":
      return "Technician reassigned";

    case "StatusChanged":
      return entry.previousStatus && entry.newStatus
        ? `Status changed from ${entry.previousStatus} to ${entry.newStatus}`
        : "Status changed";
  }
}

export default function WorkOrderHistoryControl({
  workOrder,
}: WorkOrderHistoryControlProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const [history, setHistory] = useState<WorkOrderAuditEntry[]>([]);

  const [error, setError] = useState("");

  async function openHistory() {
    try {
      setIsOpen(true);
      setIsLoading(true);
      setError("");

      const data = await getWorkOrderHistory(workOrder.id);

      setHistory(data);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to load work-order history.",
      );
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <>
      <button
        type="button"
        className="table-action-button"
        onClick={() => void openHistory()}
      >
        History
      </button>

      {isOpen && (
        <div className="modal-backdrop">
          <div
            className="edit-modal history-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="history-title"
          >
            <div className="modal-heading">
              <div>
                <p className="eyebrow">Work-order audit</p>
                <h2 id="history-title">{workOrder.title}</h2>
              </div>

              <button
                type="button"
                className="modal-close"
                aria-label="Close"
                onClick={() => setIsOpen(false)}
              >
                ×
              </button>
            </div>

            {isLoading ? (
              <p>Loading history…</p>
            ) : error ? (
              <p className="form-error">{error}</p>
            ) : history.length === 0 ? (
              <p className="empty-message">
                No audit history is available for this work order.
              </p>
            ) : (
              <ol className="audit-timeline">
                {history.map((entry) => (
                  <li key={entry.id}>
                    <span className="audit-dot" />

                    <div className="audit-entry">
                      <strong>{formatAction(entry)}</strong>

                      {(entry.action === "Assigned" ||
                        entry.action === "Reassigned") &&
                        entry.newTechnicianId && (
                          <span>Technician: {entry.newTechnicianId}</span>
                        )}

                      <span>
                        By:{" "}
                        {entry.changedByEmail ??
                          entry.changedByUserId ??
                          "System"}
                      </span>

                      <time>
                        {new Date(entry.occurredAtUtc).toLocaleString()}
                      </time>
                    </div>
                  </li>
                ))}
              </ol>
            )}
          </div>
        </div>
      )}
    </>
  );
}
