import { type FormEvent, useState } from "react";
import { createTechnician } from "./api";
import type { CreateTechnicianRequest, Technician } from "./models";

interface TechniciansPanelProps {
  technicians: Technician[];
  onCreated: () => Promise<void>;
}

const initialForm: CreateTechnicianRequest = {
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
  skills: "",
};

export default function TechniciansPanel({
  technicians,
  onCreated,
}: TechniciansPanelProps) {
  const [form, setForm] = useState<CreateTechnicianRequest>(initialForm);

  const [isSubmitting, setIsSubmitting] = useState(false);

  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function updateField(field: keyof CreateTechnicianRequest, value: string) {
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

      await createTechnician({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber?.trim() || undefined,
        skills: form.skills?.trim() || undefined,
      });

      setForm(initialForm);
      setSuccessMessage("Technician created successfully.");

      await onCreated();
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : "Unable to create technician.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="content-panel technicians-panel">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">Field team</p>
          <h2>Technicians</h2>
        </div>
      </div>

      <div className="technicians-layout">
        <form className="technician-form" onSubmit={handleSubmit}>
          <h3>Add technician</h3>

          <div className="name-fields">
            <label>
              First name
              <input
                required
                maxLength={100}
                value={form.firstName}
                onChange={(event) =>
                  updateField("firstName", event.target.value)
                }
              />
            </label>

            <label>
              Last name
              <input
                required
                maxLength={100}
                value={form.lastName}
                onChange={(event) =>
                  updateField("lastName", event.target.value)
                }
              />
            </label>
          </div>

          <label>
            Email
            <input
              required
              type="email"
              maxLength={320}
              value={form.email}
              onChange={(event) => updateField("email", event.target.value)}
            />
          </label>

          <label>
            Phone number
            <input
              type="tel"
              maxLength={30}
              value={form.phoneNumber}
              onChange={(event) =>
                updateField("phoneNumber", event.target.value)
              }
            />
          </label>

          <label>
            Skills
            <input
              maxLength={500}
              value={form.skills}
              onChange={(event) => updateField("skills", event.target.value)}
              placeholder="Plumbing, HVAC, Electrical"
            />
          </label>

          {error && <p className="form-error">{error}</p>}

          {successMessage && <p className="form-success">{successMessage}</p>}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Creating…" : "Add technician"}
          </button>
        </form>

        <div className="technician-list">
          {technicians.length === 0 ? (
            <p className="empty-message">No active technicians found.</p>
          ) : (
            technicians.map((technician) => (
              <article className="technician-card" key={technician.id}>
                <div className="technician-avatar">
                  {technician.firstName.charAt(0).toUpperCase()}
                  {technician.lastName.charAt(0).toUpperCase()}
                </div>

                <div className="technician-details">
                  <strong>
                    {technician.firstName} {technician.lastName}
                  </strong>

                  <span>{technician.email}</span>

                  {technician.phoneNumber && (
                    <span>{technician.phoneNumber}</span>
                  )}

                  {technician.skills && (
                    <span className="skills">{technician.skills}</span>
                  )}
                </div>

                <span className="active-badge">Active</span>
              </article>
            ))
          )}
        </div>
      </div>
    </section>
  );
}
