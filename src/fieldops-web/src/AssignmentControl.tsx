import { useState } from "react";
import { assignWorkOrder } from "./api";
import type { Technician, WorkOrder } from "./models";

interface AssignmentControlProps {
  workOrder: WorkOrder;
  technicians: Technician[];
  onAssigned: () => Promise<void>;
}

export default function AssignmentControl({
  workOrder,
  technicians,
  onAssigned,
}: AssignmentControlProps) {
  const [technicianId, setTechnicianId] = useState(
    workOrder.assignedTechnicianId ?? "",
  );

  const [isAssigning, setIsAssigning] = useState(false);
  const [error, setError] = useState("");

  const canAssign =
    workOrder.status === "Submitted" || workOrder.status === "Assigned";

  async function handleAssign() {
    if (!technicianId) {
      setError("Select a technician.");
      return;
    }

    try {
      setIsAssigning(true);
      setError("");

      await assignWorkOrder(workOrder.id, technicianId);

      await onAssigned();
    } catch (assignmentError) {
      setError(
        assignmentError instanceof Error
          ? assignmentError.message
          : "Unable to assign technician.",
      );
    } finally {
      setIsAssigning(false);
    }
  }

  if (!canAssign) {
    return <span className="assignment-disabled">Assignment locked</span>;
  }

  return (
    <div className="assignment-control">
      <select
        value={technicianId}
        disabled={isAssigning}
        onChange={(event) => {
          setTechnicianId(event.target.value);
          setError("");
        }}
        aria-label={`Technician for ${workOrder.title}`}
      >
        <option value="">Select technician</option>

        {technicians.map((technician) => (
          <option key={technician.id} value={technician.id}>
            {technician.firstName} {technician.lastName}
          </option>
        ))}
      </select>

      <button
        type="button"
        disabled={
          isAssigning ||
          !technicianId ||
          technicianId === workOrder.assignedTechnicianId
        }
        onClick={() => void handleAssign()}
      >
        {isAssigning
          ? "Assigning…"
          : workOrder.assignedTechnicianId
            ? "Reassign"
            : "Assign"}
      </button>

      {error && <span className="assignment-error">Assignment failed</span>}
    </div>
  );
}
