import { useState } from "react";
import { updateWorkOrderStatus } from "./api";
import type { WorkOrder, WorkOrderStatus } from "./models";

interface WorkOrderStatusControlProps {
  workOrder: WorkOrder;
  onUpdated: () => Promise<void>;
}

const allowedTransitions: Record<WorkOrderStatus, WorkOrderStatus[]> = {
  Submitted: ["Cancelled"],
  Assigned: ["InProgress", "Cancelled"],
  InProgress: ["WaitingForParts", "Completed", "Cancelled"],
  WaitingForParts: ["InProgress", "Completed", "Cancelled"],
  Completed: [],
  Cancelled: [],
};

function formatStatus(status: WorkOrderStatus): string {
  switch (status) {
    case "InProgress":
      return "In Progress";
    case "WaitingForParts":
      return "Waiting for Parts";
    default:
      return status;
  }
}

export default function WorkOrderStatusControl({
  workOrder,
  onUpdated,
}: WorkOrderStatusControlProps) {
  const transitions = allowedTransitions[workOrder.status];

  const [selectedStatus, setSelectedStatus] = useState<WorkOrderStatus | "">(
    "",
  );

  const [isUpdating, setIsUpdating] = useState(false);

  const [error, setError] = useState("");

  async function handleUpdate() {
    if (!selectedStatus) {
      return;
    }

    try {
      setIsUpdating(true);
      setError("");

      await updateWorkOrderStatus(workOrder.id, selectedStatus);

      setSelectedStatus("");
      await onUpdated();
    } catch (updateError) {
      setError(
        updateError instanceof Error
          ? updateError.message
          : "Unable to update status.",
      );
    } finally {
      setIsUpdating(false);
    }
  }

  return (
    <div className="status-control">
      <span className="badge status">{formatStatus(workOrder.status)}</span>

      {transitions.length > 0 && (
        <div className="status-actions">
          <select
            value={selectedStatus}
            disabled={isUpdating}
            onChange={(event) =>
              setSelectedStatus(event.target.value as WorkOrderStatus | "")
            }
          >
            <option value="">Change status…</option>

            {transitions.map((status) => (
              <option key={status} value={status}>
                {formatStatus(status)}
              </option>
            ))}
          </select>

          <button
            type="button"
            disabled={!selectedStatus || isUpdating}
            onClick={() => void handleUpdate()}
          >
            {isUpdating ? "Saving…" : "Update"}
          </button>
        </div>
      )}

      {error && <span className="form-error">{error}</span>}
    </div>
  );
}
