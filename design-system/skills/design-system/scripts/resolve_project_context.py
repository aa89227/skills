#!/usr/bin/env python3
"""Resolve a Design System repository role without changing the target project."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


ROLES = {
    "skill-catalog",
    "design-system",
    "consumer",
    "showcase",
    "conformance",
}

REQUIRED_FIELDS = {
    "skill-catalog": ("provides",),
    "design-system": ("designSystemId", "hosts", "sourceOfTruth"),
    "consumer": ("designSystemId", "hosts", "packages"),
    "showcase": ("designSystemId", "hosts"),
    "conformance": ("designSystemId", "hosts"),
}

ALLOWED_ARTIFACTS = {
    "skill-catalog": ["plugin manifests", "SKILL.md", "references", "Skill validation"],
    "design-system": [
        "tokens",
        "component specifications",
        "shared styles",
        "React and Blazor libraries",
        "conformance tests",
        "documentation",
    ],
    "consumer": ["application code", "integration tests", "usage documentation", "change proposals"],
    "showcase": ["live examples", "fixtures", "host-specific documentation"],
    "conformance": ["test harness", "test fixtures", "conformance reports"],
    "unknown": [],
}


def fail(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    raise SystemExit(2)


def load_manifest(root: Path) -> tuple[dict[str, Any] | None, Path | None]:
    path = root / ".design-system" / "project.json"
    if not path.exists():
        return None, None

    try:
        payload = json.loads(path.read_text())
    except json.JSONDecodeError as error:
        fail(f"invalid JSON in {path}: {error.msg}")
    except OSError as error:
        fail(f"cannot read {path}: {error}")

    if not isinstance(payload, dict):
        fail(f"{path} must contain a JSON object")

    role = payload.get("role")
    if role not in ROLES:
        fail(f"{path} has unsupported role {role!r}")

    design_system_id = payload.get("designSystemId")
    if design_system_id is not None and (
        not isinstance(design_system_id, str) or not design_system_id
    ):
        fail(f"{path}.designSystemId must be a non-empty string")

    hosts = payload.get("hosts")
    if hosts is not None and (
        not isinstance(hosts, list)
        or not hosts
        or len({host for host in hosts if isinstance(host, str)}) != len(hosts)
        or any(not isinstance(host, str) or host not in {"react", "blazor"} for host in hosts)
    ):
        fail(f"{path}.hosts must contain unique react/blazor values")

    missing = [field for field in REQUIRED_FIELDS[role] if field not in payload]
    if missing:
        fail(f"{path} is missing required field(s): {', '.join(missing)}")

    return payload, path


def package_dependencies(root: Path) -> tuple[list[str], list[str]]:
    package_path = root / "package.json"
    if not package_path.exists():
        return [], []

    try:
        payload = json.loads(package_path.read_text())
    except (OSError, json.JSONDecodeError):
        return [], ["package.json exists but could not be parsed"]

    dependencies: list[str] = []
    for section in ("dependencies", "devDependencies", "peerDependencies", "optionalDependencies"):
        values = payload.get(section, {})
        if isinstance(values, dict):
            dependencies.extend(str(name) for name in values)
    return dependencies, []


def infer_hosts(root: Path) -> list[str]:
    hosts: list[str] = []
    dependencies, _ = package_dependencies(root)
    if (root / "react").exists() or any("react" in name.lower() for name in dependencies):
        hosts.append("react")
    if (
        (root / "blazor").exists()
        or any(root.glob("**/*.razor"))
        or any(root.glob("**/*.csproj"))
    ):
        hosts.append("blazor")
    return hosts


def infer_context(root: Path) -> dict[str, Any]:
    scores = {"skill-catalog": 0, "design-system": 0, "consumer": 0}
    evidence: list[str] = []

    plugin_manifests = list(root.glob("*/.codex-plugin/plugin.json")) + list(
        root.glob("*/.claude-plugin/plugin.json")
    )
    plugin_manifests.extend(
        path
        for path in (
            root / ".codex-plugin" / "plugin.json",
            root / ".claude-plugin" / "plugin.json",
        )
        if path.exists()
    )
    has_marketplace = (root / ".agents" / "plugins" / "marketplace.json").exists() or (
        root / ".claude-plugin" / "marketplace.json"
    ).exists()
    if plugin_manifests or has_marketplace:
        scores["skill-catalog"] += 3
        evidence.append("plugin marketplace or child plugin manifests found")

    skill_files = list(root.glob("skills/*/SKILL.md"))
    if skill_files and plugin_manifests:
        scores["skill-catalog"] += 2
        evidence.append("Skill directories with SKILL.md found")

    design_directories = [
        name
        for name in ("tokens", "specs", "styles", "react", "blazor", "conformance")
        if (root / name).exists()
    ]
    if len(design_directories) >= 3 or {"tokens", "specs"}.issubset(design_directories):
        scores["design-system"] += 4
        evidence.append(f"Design System source directories found: {', '.join(design_directories)}")

    dependencies, dependency_evidence = package_dependencies(root)
    evidence.extend(dependency_evidence)
    dependency_pattern = re.compile(r"design[-.]?system|component[-.]?library", re.IGNORECASE)
    design_dependencies = [name for name in dependencies if dependency_pattern.search(name)]
    if design_dependencies:
        scores["consumer"] += 4
        evidence.append(f"Design System package references found: {', '.join(sorted(design_dependencies))}")

    project_files = list(root.glob("**/*.csproj"))
    project_reference_hits: list[str] = []
    for project_file in project_files:
        try:
            contents = project_file.read_text()
        except OSError:
            continue
        if dependency_pattern.search(contents):
            project_reference_hits.append(project_file.name)
    if project_reference_hits:
        scores["consumer"] += 4
        evidence.append(
            f"Design System PackageReference markers found: {', '.join(sorted(project_reference_hits))}"
        )

    ranked = sorted(scores.items(), key=lambda item: item[1], reverse=True)
    top_role, top_score = ranked[0]
    second_score = ranked[1][1]
    if top_score == 0 or top_score == second_score:
        role = "unknown"
        confidence = "low"
        if top_score and top_score == second_score:
            evidence.append("role signals conflict or are tied")
        else:
            evidence.append("no reliable role signal found")
    else:
        role = top_role
        confidence = "high" if top_score >= 5 and top_score > second_score + 2 else "medium"

    return {
        "targetRoot": str(root),
        "role": role,
        "confidence": confidence,
        "hosts": infer_hosts(root),
        "evidence": evidence,
        "allowedArtifacts": ALLOWED_ARTIFACTS[role],
    }


def resolve(root: Path) -> dict[str, Any]:
    manifest, manifest_path = load_manifest(root)
    if manifest is not None:
        role = str(manifest["role"])
        return {
            "targetRoot": str(root),
            "role": role,
            "confidence": "high",
            "designSystemId": manifest.get("designSystemId"),
            "hosts": manifest.get("hosts", []),
            "evidence": [f"explicit project manifest: {manifest_path}"],
            "allowedArtifacts": ALLOWED_ARTIFACTS[role],
            "manifest": manifest,
        }

    return infer_context(root)


def render_text(context: dict[str, Any]) -> str:
    lines = [
        f"Project role: {context['role']}",
        f"Confidence: {context['confidence']}",
        f"Target root: {context['targetRoot']}",
        f"Hosts: {', '.join(context['hosts']) or 'unknown'}",
        "Evidence:",
    ]
    lines.extend(f"- {item}" for item in context["evidence"])
    lines.append("Allowed artifacts: " + (", ".join(context["allowedArtifacts"]) or "none until resolved"))
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target", nargs="?", default=".", help="Target repository root (default: current directory)")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args()

    root = Path(args.target).expanduser().resolve()
    if not root.is_dir():
        fail(f"target is not a directory: {root}")

    context = resolve(root)
    if args.format == "json":
        print(json.dumps(context, indent=2))
    else:
        print(render_text(context))
    return 0


if __name__ == "__main__":
    sys.exit(main())
