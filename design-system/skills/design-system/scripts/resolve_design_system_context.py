#!/usr/bin/env python3
"""Resolve the read-only context used to route Design System work."""

from __future__ import annotations

import argparse
from contextlib import redirect_stderr
from io import StringIO
import json
import sys
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))

from inspect_design_system_manifest import resolve as resolve_catalog  # noqa: E402
from resolve_project_context import resolve as resolve_project  # noqa: E402


MODES = {"design", "build", "consume", "conformance", "release"}


def host_context(project: dict[str, Any], requested: str | None) -> dict[str, Any]:
    hosts = project.get("hosts", [])
    if requested:
        if requested in hosts:
            return {"status": "selected", "requested": requested, "selected": requested}
        return {"status": "unsupported", "requested": requested, "selected": None}
    if len(hosts) == 1:
        return {"status": "selected", "requested": None, "selected": hosts[0]}
    if len(hosts) > 1:
        return {"status": "ambiguous", "requested": None, "selected": None}
    return {"status": "not-requested", "requested": None, "selected": None}


def catalog_context(catalog: dict[str, Any] | None) -> dict[str, Any]:
    if catalog is None:
        return {
            "status": "not-requested",
            "provider": None,
            "manifest": None,
            "systemId": None,
            "displayName": None,
            "installedRelease": None,
            "contractVersion": None,
            "hosts": [],
            "packages": None,
            "styles": None,
            "documentation": None,
            "conformance": None,
            "componentCount": None,
            "component": None,
            "candidates": [],
            "latest": None,
            "updateStatus": "unknown",
            "error": None,
        }

    status = catalog.get("status")
    latest = catalog.get("latest")
    update_status = "unknown"
    if isinstance(latest, dict):
        latest_status = latest.get("status")
        if latest_status == "ambiguous":
            update_status = "ambiguous"
        elif latest_status == "not-found":
            update_status = "not-found"
        elif latest_status == "found":
            same_system = latest.get("systemId") in {None, catalog.get("systemId")}
            same_release = latest.get("releaseVersion") == catalog.get("releaseVersion")
            same_contract = latest.get("contractVersion") == catalog.get("contractVersion")
            if same_system and same_release and same_contract:
                update_status = "current"
            elif same_system:
                update_status = "candidate-available"
            else:
                update_status = "different-system"
    return {
        "status": status if status in {"not-found", "ambiguous", "found", "component-not-found"} else "invalid",
        "provider": catalog.get("provider", "manifest"),
        "manifest": catalog.get("manifest"),
        "systemId": catalog.get("systemId"),
        "displayName": catalog.get("displayName"),
        "installedRelease": catalog.get("releaseVersion"),
        "contractVersion": catalog.get("contractVersion"),
        "hosts": catalog.get("hosts", []),
        "packages": catalog.get("packages"),
        "styles": catalog.get("styles"),
        "documentation": catalog.get("documentation"),
        "conformance": catalog.get("conformance"),
        "componentCount": catalog.get("componentCount"),
        "component": catalog.get("component"),
        "candidates": catalog.get("candidates", []),
        "latest": latest,
        "updateStatus": update_status,
        "error": catalog.get("error"),
    }


def select_outcome(
    project: dict[str, Any],
    mode: str,
    host: dict[str, Any],
    catalog: dict[str, Any],
    component_query: str | None,
    candidate_manifest: bool,
) -> dict[str, Any]:
    role = project.get("role")
    if role == "skill-catalog":
        return {
            "outcome": "skill-catalog-task",
            "canProceed": True,
            "reason": "The target repository owns Skills and plugin artifacts, not runtime Design System packages.",
            "requiredChanges": [],
            "resumeCondition": None,
        }
    if role == "unknown" or host["status"] == "ambiguous":
        return {
            "outcome": "blocked-on-context",
            "canProceed": False,
            "reason": "The target project or active host is ambiguous.",
            "requiredChanges": ["Declare the project role and active host in project context."],
            "resumeCondition": "A single target project and host are resolved.",
        }
    if host["status"] == "unsupported":
        return {
            "outcome": "blocked-on-host-support",
            "canProceed": False,
            "reason": "The requested host is not declared by the target project.",
            "requiredChanges": ["Add the host to project context or select a supported host."],
            "resumeCondition": "The requested host is explicitly supported by the target project.",
        }
    project_system_id = project.get("designSystemId")
    catalog_system_id = catalog.get("systemId")
    if (
        catalog["status"] in {"found", "component-not-found"}
        and project_system_id
        and catalog_system_id
        and project_system_id != catalog_system_id
    ):
        return {
            "outcome": "blocked-on-context",
            "canProceed": False,
            "reason": "The project Design System ID does not match the installed catalog.",
            "requiredChanges": ["Select a manifest for the declared Design System or update project context."],
            "resumeCondition": "Project context and installed catalog declare the same Design System ID.",
        }
    if role == "design-system" and mode in {"design", "build"}:
        return {
            "outcome": "design-system-task",
            "canProceed": True,
            "reason": "The target owns the shared contract and host implementations for this task.",
            "requiredChanges": [],
            "resumeCondition": None,
        }
    if role == "design-system" and mode == "release":
        if candidate_manifest and catalog["status"] != "found":
            return {
                "outcome": "blocked-on-catalog",
                "canProceed": False,
                "reason": "The release candidate manifest could not be resolved and validated.",
                "requiredChanges": ["Generate or provide a valid release candidate manifest."],
                "resumeCondition": "The candidate manifest is found and validated against the manifest schema.",
            }
        return {
            "outcome": "release-task",
            "canProceed": True,
            "reason": "The target owns the shared release and must validate the candidate manifest and host artifacts.",
            "requiredChanges": [],
            "resumeCondition": None,
        }
    if role == "showcase" and mode in {"design", "build"}:
        return {
            "outcome": "showcase-task",
            "canProceed": True,
            "reason": "The target owns host-specific examples and fixtures, not the shared contract.",
            "requiredChanges": [],
            "resumeCondition": None,
        }
    if mode == "conformance":
        return {
            "outcome": "conformance-required",
            "canProceed": catalog["status"] == "found",
            "reason": "Conformance work must compare the supported host implementations against the contract.",
            "requiredChanges": [] if catalog["status"] == "found" else ["Resolve the exact Design System catalog."],
            "resumeCondition": "The exact catalog and supported hosts are available.",
        }
    if mode not in {"consume", "release"}:
        return {
            "outcome": "resolved-in-consumer" if role == "consumer" else "conformance-required",
            "canProceed": True,
            "reason": f"The {mode} mode does not require component catalog resolution to route the task.",
            "requiredChanges": [],
            "resumeCondition": None,
        }
    if catalog["status"] in {"not-found", "ambiguous", "invalid"}:
        return {
            "outcome": "blocked-on-catalog",
            "canProceed": False,
            "reason": "The exact installed Design System catalog could not be resolved.",
            "requiredChanges": ["Resolve an explicit, valid package manifest through a Design System provider."],
            "resumeCondition": "The installed release manifest is found and validated.",
        }
    if component_query and catalog["status"] == "component-not-found":
        latest = catalog.get("latest")
        if isinstance(latest, dict) and latest.get("status") == "found" and latest.get("component"):
            return {
                "outcome": "blocked-on-package-upgrade",
                "canProceed": False,
                "reason": f"Component '{component_query}' is available in a newer catalog, not the installed release.",
                "requiredChanges": ["Request or perform an explicit compatible Design System package upgrade."],
                "resumeCondition": "The consumer resolves a release whose catalog exposes the requested component.",
            }
        return {
            "outcome": "blocked-on-component-contract",
            "canProceed": False,
            "reason": f"Component '{component_query}' is not present in the installed Design System catalog.",
            "requiredChanges": [
                "Check an explicit compatible release candidate.",
                "If still absent, submit a Component Specification change proposal.",
            ],
            "resumeCondition": "The installed catalog exposes the requested component or an approved consumer workaround is selected.",
        }
    if host["selected"] not in catalog["hosts"]:
        return {
            "outcome": "blocked-on-host-support",
            "canProceed": False,
            "reason": f"The installed catalog does not declare host '{host['selected']}'.",
            "requiredChanges": ["Add host support to the Design System or use a supported host."],
            "resumeCondition": "The catalog declares the active host.",
        }
    component = catalog.get("component")
    if component_query and component is not None:
        host_apis = component.get("hostApis", {})
        if host["selected"] not in host_apis:
            latest = catalog.get("latest")
            latest_component = latest.get("component") if isinstance(latest, dict) else None
            latest_host_apis = latest_component.get("hostApis", {}) if isinstance(latest_component, dict) else {}
            if isinstance(latest, dict) and latest.get("status") == "found" and host["selected"] in latest_host_apis:
                return {
                    "outcome": "blocked-on-package-upgrade",
                    "canProceed": False,
                    "reason": f"Component '{component['id']}' supports host '{host['selected']}' only in a newer catalog.",
                    "requiredChanges": ["Request or perform an explicit compatible Design System package upgrade."],
                    "resumeCondition": "The consumer resolves a release whose component mapping supports the active host.",
                }
            return {
                "outcome": "blocked-on-host-support",
                "canProceed": False,
                "reason": f"Component '{component['id']}' has no '{host['selected']}' API mapping.",
                "requiredChanges": ["Implement or document the component for the active host."],
                "resumeCondition": "The component exposes a host API mapping for the active host.",
            }
    return {
        "outcome": "resolved-in-consumer",
        "canProceed": True,
        "reason": "The installed catalog and active host provide enough information for usage guidance.",
        "requiredChanges": [],
        "resumeCondition": None,
    }


def resolve_context(
    root: Path,
    mode: str,
    host: str | None,
    component_query: str | None,
    manifest: str | None,
    latest_manifest: str | None,
) -> dict[str, Any]:
    project = resolve_project(root)
    selected_host = host_context(project, host)
    should_resolve_catalog = (
        mode in {"consume", "conformance"}
        or component_query is not None
        or manifest is not None
        or latest_manifest is not None
    )
    raw_catalog = None
    if should_resolve_catalog:
        diagnostics = StringIO()
        try:
            with redirect_stderr(diagnostics):
                raw_catalog = resolve_catalog(root, manifest, component_query, latest_manifest)
        except SystemExit as error:
            raw_catalog = {
                "status": "invalid",
                "targetRoot": str(root),
                "provider": "manifest",
                "error": diagnostics.getvalue().strip()
                or f"catalog provider failed with exit code {error.code}",
            }
    catalog = catalog_context(raw_catalog)
    decision = select_outcome(
        project,
        mode,
        selected_host,
        catalog,
        component_query,
        manifest is not None,
    )
    return {
        "project": {
            "targetRoot": project.get("targetRoot", str(root)),
            "role": project.get("role", "unknown"),
            "confidence": project.get("confidence", "low"),
            "designSystemId": project.get("designSystemId"),
            "hosts": project.get("hosts", []),
            "evidence": project.get("evidence", []),
            "allowedArtifacts": project.get("allowedArtifacts", []),
        },
        "task": {
            "mode": mode,
            "componentQuery": component_query,
        },
        "catalog": catalog,
        "host": selected_host,
        "decision": decision,
    }


def render_text(context: dict[str, Any]) -> str:
    project = context["project"]
    task = context["task"]
    catalog = context["catalog"]
    decision = context["decision"]
    lines = [
        f"Project role: {project['role']}",
        f"Task mode: {task['mode']}",
        f"Target root: {project['targetRoot']}",
        f"Host: {context['host']['selected'] or context['host']['status']}",
        f"Catalog: {catalog['status']}",
        f"Catalog provider: {catalog['provider'] or 'none'}",
        f"Outcome: {decision['outcome']}",
        f"Can proceed: {'yes' if decision['canProceed'] else 'no'}",
        f"Reason: {decision['reason']}",
    ]
    if catalog.get("displayName"):
        lines.extend(
            [
                f"Design System: {catalog['displayName']} ({catalog['systemId']})",
                f"Installed release: {catalog['installedRelease']}",
                f"Contract version: {catalog['contractVersion']}",
            ]
        )
    if catalog.get("component"):
        component = catalog["component"]
        lines.append(f"Component: {component['displayName']} ({component['id']})")
    if catalog.get("styles"):
        styles = catalog["styles"]
        lines.append(f"Styles: {styles.get('package')} -> {styles.get('entrypoint')}")
    if catalog.get("documentation"):
        lines.append(f"Documentation: {catalog['documentation']}")
    if catalog.get("updateStatus") not in {None, "unknown"}:
        lines.append(f"Catalog update: {catalog['updateStatus']}")
    latest = catalog.get("latest")
    if isinstance(latest, dict) and latest.get("status") == "found":
        lines.append(
            f"Latest catalog: {latest.get('releaseVersion')} "
            f"(contract {latest.get('contractVersion')})"
        )
    if catalog.get("error"):
        lines.append(f"Catalog error: {catalog['error']}")
    if decision["requiredChanges"]:
        lines.append("Required changes:")
        lines.extend(f"- {change}" for change in decision["requiredChanges"])
    if decision["resumeCondition"]:
        lines.append(f"Resume condition: {decision['resumeCondition']}")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target", nargs="?", default=".", help="Target repository root")
    parser.add_argument("--mode", required=True, choices=sorted(MODES))
    parser.add_argument("--host", choices=("react", "blazor"))
    parser.add_argument("--component", help="Component ID, alias, or unique display name")
    parser.add_argument("--manifest", help="Explicit installed manifest path")
    parser.add_argument("--latest-manifest", help="Optional local latest manifest path")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args()

    root = Path(args.target).expanduser().resolve()
    if not root.is_dir():
        print(f"error: target is not a directory: {root}", file=sys.stderr)
        return 2
    context = resolve_context(root, args.mode, args.host, args.component, args.manifest, args.latest_manifest)
    if args.format == "json":
        print(json.dumps(context, indent=2))
    else:
        print(render_text(context))
    return 0


if __name__ == "__main__":
    sys.exit(main())
