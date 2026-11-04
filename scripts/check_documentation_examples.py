"""Verify the README inheritance example is exactly the file compiled by tests."""

from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
readme = (root / "README.md").read_text(encoding="utf-8")
source_path = root / "DimonSmart.Specification.Tests/Documentation/CustomStudentSpecification.cs"

match = re.search(
    r"<!-- compiled-example: CustomStudentSpecification -->\s*```csharp\n(.*?)\n```",
    readme,
    re.DOTALL,
)
if match is None:
    raise SystemExit("README inheritance code block is missing")
if match.group(1).strip() != source_path.read_text(encoding="utf-8").strip():
    raise SystemExit("README inheritance code differs from compiled source")
print("README inheritance example matches compiled C# source")
