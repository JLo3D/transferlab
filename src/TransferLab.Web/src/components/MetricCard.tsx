interface MetricCardProps {
  label: string;
  value: string;
  detail: string;
  tone?: "default" | "accent" | "positive";
}

export function MetricCard({
  label,
  value,
  detail,
  tone = "default",
}: MetricCardProps) {
  return (
    <article className={`metric-card metric-${tone}`}>
      <p className="eyebrow">{label}</p>
      <strong>{value}</strong>
      <span>{detail}</span>
    </article>
  );
}
