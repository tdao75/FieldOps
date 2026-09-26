import { type FormEvent, useState } from "react";
import { updateWorkOrderDetails } from "./api";
import type {
  UpdateWorkOrderDetailsRequest,
  WorkOrder,
  WorkOrderPriority,
} from "./models";

interface EditWorkOrderControlProps {
  workOrder: WorkOrder;
  onUpdated: () => Promise<void>;
}

export default function EditWorkOrderControl({
  workOrder,
  onUpdated,
}: EditWorkOrderControlProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");

  const [form, setForm] = useState<UpdateWorkOrderDetailsRequest>({
    title: workOrder.title,
    description: workOrder.description ?? "",
    location: workOrder.location,
    priority: workOrder.priority,
  });

  const isLocked =
    workOrder.status === "Completed" || workOrder.status === "Cancelled";

  function openEditor() {
    setForm({
      title: workOrder.title,
      description: workOrder.description ?? "",
      location: workOrder.location,
      priority: workOrder.priority,
    });

    setError("");
    setIsOpen(true);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      setIsSaving(true);
      setError("");

      await updateWorkOrderDetails(workOrder.id, {
        title: form.title.trim(),
        description: form.description?.trim() || undefined,
        location: form.location.trim(),
        priority: form.priority,
      });

      setIsOpen(false);
      await onUpdated();
    } catch (updateError) {
      setError(
        updateError instanceof Error
          ? updateError.message
          : "Unable to update work order.",
      );
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <>
      <button
        type="button"
        className="table-action-button"
        disabled={isLocked}
        title={
          isLocked
            ? "Completed or cancelled work orders cannot be edited."
            : "Edit work order"
        }
        onClick={openEditor}
      >
        Edit
      </button>

      {isOpen && (
        <div className="modal-backdrop">
          <div
            className="edit-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="edit-work-order-title"
          >
            <div className="modal-heading">
              <h2 id="edit-work-order-title">Edit work order</h2>

              <button
                type="button"
                className="modal-close"
                aria-label="Close"
                onClick={() => setIsOpen(false)}
              >
                ×
              </button>
            </div>

            <form onSubmit={handleSubmit}>
              <label>
                Title
                <input
                  required
                  maxLength={150}
                  value={form.title}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      title: event.target.value,
                    }))
                  }
                />
              </label>

              <label>
                Description
                <textarea
                  maxLength={2000}
                  value={form.description ?? ""}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      description: event.target.value,
                    }))
                  }
                />
              </label>

              <label>
                Location
                <input
                  required
                  maxLength={250}
                  value={form.location}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      location: event.target.value,
                    }))
                  }
                />
              </label>

              <label>
                Priority
                <select
                  value={form.priority}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      priority: event.target.value as WorkOrderPriority,
                    }))
                  }
                >
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Emergency">Emergency</option>
                </select>
              </label>

              {error && <p className="form-error">{error}</p>}

              <div className="modal-actions">
                <button
                  type="button"
                  onClick={() => setIsOpen(false)}
                  disabled={isSaving}
                >
                  Cancel
                </button>

                <button type="submit" disabled={isSaving}>
                  {isSaving ? "Saving…" : "Save changes"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
