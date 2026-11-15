"""Validate published C# examples against compiled sources and detect stale docs links."""

from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
readme = (root / "README.md").read_text(encoding="utf-8")
showcase = (root / "SHOWCASE.md").read_text(encoding="utf-8")

if (root / "docs").exists():
    raise SystemExit("The legacy docs/ directory must be migrated into .idd/intent/")
if "docs/" in readme or "docs/" in showcase:
    raise SystemExit("Public Markdown still links to removed docs/ pages")

examples = {
    "DynamicSpecificationExample": root / "DimonSmart.Specification.Tests/Documentation/DynamicSpecificationExample.cs",
    "DynamicEFCoreSpecificationExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/DynamicEFCoreSpecificationExample.cs",
    "CustomStudentSpecification": root / "DimonSmart.Specification.Tests/Documentation/CustomStudentSpecification.cs",
    "CustomEFCoreStudentSpecification": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/CustomEFCoreStudentSpecification.cs",
    "FilteredIncludeExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/FilteredIncludeExample.cs",
    "ProjectionExample": root / "DimonSmart.Specification.Tests/Documentation/ProjectionExample.cs",
    "EFCoreProjectionExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/EFCoreProjectionExample.cs",
    "NamedQueryFiltersExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/NamedQueryFiltersExample.cs",
    "TrackingExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/TrackingExample.cs",
    "KeysetPaginationExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/KeysetPaginationExample.cs",
    "ConditionalWhereExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/ConditionalWhereExample.cs",
    "TypedOrderingExample": root / "DimonSmart.Specification.Tests/Documentation/TypedOrderingExample.cs",
    "EnumerableBySpecificationExample": root / "DimonSmart.Specification.Tests/Documentation/EnumerableBySpecificationExample.cs",
    "CriteriaOnlyEvaluationExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/CriteriaOnlyEvaluationExample.cs",
    "QueryableEFCoreExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/QueryableEFCoreExample.cs",
}
checked = (
    "KeysetPaginationExample",
    "FilteredIncludeExample",
    "QueryableEFCoreExample",
)

for name, source in examples.items():
    if not source.is_file():
        raise SystemExit(f"Compiled example {name} is missing: {source}")
    relative_path = source.relative_to(root).as_posix()
    if f"({relative_path})" not in showcase:
        raise SystemExit(f"Missing showcase link to compiled example {name}")

for name in checked:
    marker = "<!-- compiled-example: " + name + " -->"
    match = re.search(
        re.escape(marker) + r"\s*\x60{3}csharp\n(.*?)\n\x60{3}",
        showcase,
        re.DOTALL,
    )
    if match is None:
        raise SystemExit(f"Checked showcase snippet {name} is missing")
    if match.group(1).strip() != examples[name].read_text(encoding="utf-8").strip():
        raise SystemExit(f"Checked showcase snippet {name} differs from compiled source")

print(f"Validated {len(examples)} compiled example links and {len(checked)} inline snippets")
