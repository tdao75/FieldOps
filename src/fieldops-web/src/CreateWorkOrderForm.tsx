import { type FormEvent, useState } from "react";
import { createWorkOrder } from "./api";
import type { CreateWorkOrderRequest, WorkOrderPriority } from "./models";

interface CreateWorkOrderFormProps {
  onCreated: () => Promise<void>;
}

const initialForm: CreateWorkOrderRequest = {
  title: "",
  description: "",
  location: "",
  priority: "Medium",
};

export default function CreateWorkOrderForm({
  onCreated,
}: CreateWorkOrderFormProps) {
  const [form, setForm] = useState<CreateWorkOrderRequest>(initialForm);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function updateField(field: keyof CreateWorkOrderRequest, value: string) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      setIsSubmitting(true);
      setError("");
      setSuccessMessage("");

      await createWorkOrder({
        title: form.title.trim(),
        description: form.description?.trim(),
        location: form.location.trim(),
        priority: form.priority,
      });

      setForm(initialForm);
      setSuccessMessage("Work order created successfully.");

      await onCreated();
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : "Unable to create the work order.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="content-panel create-panel">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">New request</p>
          <h2>Create work order</h2>
        </div>
      </div>

      <form className="work-order-form" onSubmit={handleSubmit}>
        <label>
          Title
          <input
            required
            maxLength={150}
            value={form.title}
            onChange={(event) => updateField("title", event.target.value)}
            placeholder="Example: Broken lobby door"
          />
        </label>

        <label>
          Location
          <input
            required
            maxLength={250}
            value={form.location}
            onChange={(event) => updateField("location", event.target.value)}
            placeholder="Example: Building A, lobby"
          />
        </label>

        <label>
          Priority
          <select
            value={form.priority}
            onChange={(event) =>
              updateField("priority", event.target.value as WorkOrderPriority)
            }
          >
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Emergency">Emergency</option>
          </select>
        </label>

        <label className="full-width">
          Description
          <textarea
            maxLength={2000}
            rows={4}
            value={form.description}
            onChange={(event) => updateField("description", event.target.value)}
            placeholder="Describe the problem and any relevant details"
          />
        </label>

        <div className="form-footer full-width">
          <div>
            {error && <p className="form-error">{error}</p>}

            {successMessage && <p className="form-success">{successMessage}</p>}
          </div>

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Creating…" : "Create work order"}
          </button>
        </div>
      </form>
    </section>
  );
}
