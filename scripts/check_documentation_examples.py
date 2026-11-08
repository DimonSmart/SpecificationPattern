"""Verify API reference code blocks match their compiled C# sources."""

from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
readme = (root / "docs" / "REFERENCE.md").read_text(encoding="utf-8")
examples = {
    "DynamicSpecificationExample": root / "DimonSmart.Specification.Tests/Documentation/DynamicSpecificationExample.cs",
    "DynamicEFCoreSpecificationExample": root / "DimonSmart.Specification.EntityFrameworkCore.Db.Tests/Documentation/DynamicEFCoreSpecificationExample.cs",
    "CustomStudentSpecification": root / "DimonSmart.Specification.Tests/Documentation/CustomStudentSpecification.cs",
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

for name, source in examples.items():
    marker = "<!-- compiled-example: " + name + " -->"
    match = re.search(re.escape(marker) + r"\s*\x60{3}csharp\n(.*?)\n\x60{3}", readme, re.DOTALL)
    if match is None:
        raise SystemExit("API reference " + name + " example missing")
    if match.group(1).strip() != source.read_text(encoding="utf-8").strip():
        raise SystemExit("API reference " + name + " does not match source")

print("API reference examples match compiled C# source files")
