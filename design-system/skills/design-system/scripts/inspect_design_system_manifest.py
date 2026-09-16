#!/usr/bin/env python3
"""Inspect a versioned Design System package manifest without changing a project."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


STATUSES = {"experimental", "stable", "deprecated", "removed"}
HOSTS = {"react", "blazor"}
RELEASE_VERSION = re.compile(
    r"^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$"
)
COMPONENT_ID = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")


def fail(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    raise SystemExit(2)


def load_json(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text())
    except json.JSONDecodeError as error:
        fail(f"invalid JSON in {path}: {error.msg}")
    except OSError as error:
        fail(f"cannot read {path}: {error}")
    if not isinstance(payload, dict):
        fail(f"{path} must contain a JSON object")
    return payload


def validate_manifest(manifest: dict[str, Any], path: Path) -> None:
    required = (
        "schemaVersion",
        "systemId",
        "displayName",
        "releaseVersion",
        "contractVersion",
        "hosts",
        "components",
    )
    missing = [field for field in required if field not in manifest]
    if missing:
        fail(f"{path} is missing required field(s): {', '.join(missing)}")

    if not isinstance(manifest["schemaVersion"], str) or not manifest["schemaVersion"]:
        fail(f"{path}.schemaVersion must be a non-empty string")
    if not isinstance(manifest["systemId"], str) or not manifest["systemId"]:
        fail(f"{path}.systemId must be a non-empty string")
    if not isinstance(manifest["displayName"], str) or not manifest["displayName"]:
        fail(f"{path}.displayName must be a non-empty string")
    if not isinstance(manifest["releaseVersion"], str) or not RELEASE_VERSION.fullmatch(
        manifest["releaseVersion"]
    ):
        fail(f"{path}.releaseVersion must be a semantic version")
    if not isinstance(manifest["contractVersion"], str) or not manifest["contractVersion"]:
        fail(f"{path}.contractVersion must be a non-empty string")

    hosts = manifest["hosts"]
    if (
        not isinstance(hosts, list)
        or not hosts
        or len(set(hosts)) != len(hosts)
        or any(host not in HOSTS for host in hosts)
    ):
        fail(f"{path}.hosts must contain one or more of: react, blazor")

    components = manifest["components"]
    if not isinstance(components, list) or not components:
        fail(f"{path}.components must be a non-empty array")

    component_ids: set[str] = set()
    for index, component in enumerate(components):
        if not isinstance(component, dict):
            fail(f"{path}.components[{index}] must be an object")
        component_required = ("id", "displayName", "status", "contract")
        missing_component = [field for field in component_required if field not in component]
        if missing_component:
            fail(
                f"{path}.components[{index}] is missing required field(s): "
                + ", ".join(missing_component)
            )
        component_id = component["id"]
        if not isinstance(component_id, str) or not COMPONENT_ID.fullmatch(component_id):
            fail(f"{path}.components[{index}].id must be kebab-case")
        if component_id in component_ids:
            fail(f"{path}.components[{index}].id is duplicated: {component_id}")
        component_ids.add(component_id)
        if not isinstance(component["displayName"], str) or not component["displayName"]:
            fail(f"{path}.components[{index}].displayName must be a non-empty string")
        if component["status"] not in STATUSES:
            fail(f"{path}.components[{index}].status is unsupported")
        host_apis = component.get("hostApis", {})
        if not isinstance(host_apis, dict) or any(host not in hosts for host in host_apis):
            fail(f"{path}.components[{index}].hostApis contains an unsupported host")
        for host, api in host_apis.items():
            if not isinstance(api, dict) or not api.get("package") or not (
                api.get("export") or api.get("type")
            ):
                fail(f"{path}.components[{index}].hostApis.{host} must declare package and export/type")
        scenarios = component.get("scenarios", [])
        if not isinstance(scenarios, list) or any(
            not isinstance(scenario, str) or not scenario for scenario in scenarios
        ):
            fail(f"{path}.components[{index}].scenarios must contain non-empty strings")

    conformance = manifest.get("conformance")
    if conformance is not None and (
        not isinstance(conformance, dict) or not conformance.get("scenarioSource")
    ):
        fail(f"{path}.conformance must declare scenarioSource")


def read_context(root: Path) -> dict[str, Any] | None:
    context_path = root / ".design-system" / "project.json"
    if not context_path.exists():
        return None
    return load_json(context_path)


def configured_package_names(root: Path) -> list[str]:
    context = read_context(root)
    packages = context.get("packages") if isinstance(context, dict) else None
    if not isinstance(packages, dict):
        return []
    return [str(value) for value in packages.values() if isinstance(value, str)]


def local_registry_manifests(root: Path) -> list[Path]:
    """Return manifests from a configured local registry snapshot, if any.

    A registry is an update source, not evidence of the installed package, so these paths are
    intentionally used only by latest-catalog discovery.
    """

    context = read_context(root)
    catalog = context.get("catalog") if isinstance(context, dict) else None
    raw_registry = catalog.get("registry") if isinstance(catalog, dict) else None
    if not isinstance(raw_registry, str) or raw_registry.startswith(("http://", "https://")):
        return []

    registry = Path(raw_registry).expanduser()
    if not registry.is_absolute():
        registry = root / registry
    registry = registry.resolve()
    if registry.is_file():
        return [registry] if registry.name == "design-system.manifest.json" else []
    if not registry.is_dir():
        return []
    return sorted(registry.rglob("design-system.manifest.json"))


def npm_manifest_candidates(root: Path) -> list[Path]:
    node_modules = root / "node_modules"
    if not node_modules.is_dir():
        return []

    candidates: list[Path] = []
    configured_names = configured_package_names(root)

    def add_package_manifests(package_name: str) -> None:
        package_root = node_modules / package_name
        if not package_root.is_dir():
            return
        candidates.extend(sorted(package_root.rglob("design-system.manifest.json")))

    for package_name in configured_names:
        if package_name.startswith("@") and package_name.count("/") >= 1:
            add_package_manifests(package_name)
        elif not package_name.startswith("."):
            add_package_manifests(package_name)

    # Keep the fallback narrow: a package manifest is expected at the package root or one level
    # below a scoped package. Explicit project context is preferred when a repository has several
    # Design System packages installed.
    candidates.extend(sorted(node_modules.glob("*/design-system.manifest.json")))
    candidates.extend(sorted(node_modules.glob("@*/*/design-system.manifest.json")))
    return candidates


def nuget_manifest_candidates(root: Path) -> list[Path]:
    """Find manifests bundled in packages resolved by project.assets.json.

    This is deliberately local and read-only. It does not restore packages or contact NuGet. A
    private feed can still provide an explicit catalog.manifest or a local registry snapshot.
    """

    asset_files = [root / "obj" / "project.assets.json"]
    asset_files.extend(sorted(root.glob("*/obj/project.assets.json")))
    asset_files.extend(sorted(root.glob("**/obj/project.assets.json")))
    unique_asset_files = list(dict.fromkeys(path.resolve() for path in asset_files if path.is_file()))
    candidates: list[Path] = []

    for asset_file in unique_asset_files:
        try:
            assets = json.loads(asset_file.read_text())
        except (OSError, json.JSONDecodeError):
            continue
        if not isinstance(assets, dict):
            continue
        package_folders = assets.get("packageFolders", {})
        libraries = assets.get("libraries", {})
        if not isinstance(package_folders, dict) or not isinstance(libraries, dict):
            continue

        for library_key, library in libraries.items():
            if not isinstance(library, dict) or library.get("type") != "package":
                continue
            package_path = library.get("path")
            if not isinstance(package_path, str):
                package_path = str(library_key)
            for folder in package_folders:
                package_root = Path(str(folder)).expanduser() / package_path
                if not package_root.is_dir():
                    continue
                candidates.extend(sorted(package_root.rglob("design-system.manifest.json")))

    return candidates


def candidate_sources(root: Path, explicit: str | None) -> list[tuple[Path, str]]:
    candidates: list[tuple[Path, str]] = []

    def add(raw_path: str, provider: str) -> None:
        path = Path(raw_path).expanduser()
        if not path.is_absolute():
            path = root / path
        path = path.resolve()
        if not any(existing == path for existing, _ in candidates):
            candidates.append((path, provider))

    if explicit:
        add(explicit, "explicit-manifest")
        return candidates

    context = read_context(root)
    catalog = context.get("catalog") if isinstance(context, dict) else None
    if isinstance(catalog, dict) and isinstance(catalog.get("manifest"), str):
        add(catalog["manifest"], "project-context")
        return candidates

    add(".design-system/design-system.manifest.json", "project-manifest")
    add("design-system.manifest.json", "project-manifest")

    for path in npm_manifest_candidates(root):
        add(str(path), "npm")
    for path in nuget_manifest_candidates(root):
        add(str(path), "nuget")

    return candidates


def candidate_paths(root: Path, explicit: str | None) -> list[Path]:
    return [path for path, _ in candidate_sources(root, explicit)]


def find_component(manifest: dict[str, Any], query: str) -> dict[str, Any] | None:
    normalized = query.casefold()
    exact = [component for component in manifest["components"] if component.get("id") == query]
    if len(exact) == 1:
        return exact[0]

    aliases = [
        component
        for component in manifest["components"]
        if normalized in {str(alias).casefold() for alias in component.get("aliases", [])}
    ]
    if len(aliases) == 1:
        return aliases[0]

    display_names = [
        component
        for component in manifest["components"]
        if str(component.get("displayName", "")).casefold() == normalized
    ]
    if len(display_names) == 1:
        return display_names[0]

    return None


def configured_latest_manifest(root: Path, explicit: str | None) -> Path | None:
    raw_path = explicit
    if raw_path is None:
        context = read_context(root)
        catalog = context.get("catalog") if isinstance(context, dict) else None
        if isinstance(catalog, dict) and isinstance(catalog.get("latestManifest"), str):
            raw_path = catalog["latestManifest"]
    if raw_path is None:
        return None
    path = Path(raw_path).expanduser()
    if not path.is_absolute():
        path = root / path
    return path.resolve()


def latest_summary(
    manifest_path: Path,
    component_query: str | None,
    provider: str | None = None,
) -> dict[str, Any]:
    manifest = load_json(manifest_path)
    validate_manifest(manifest, manifest_path)
    result: dict[str, Any] = {
        "status": "found",
        "manifest": str(manifest_path),
        "systemId": manifest["systemId"],
        "releaseVersion": manifest["releaseVersion"],
        "contractVersion": manifest["contractVersion"],
    }
    if provider:
        result["provider"] = provider
    if component_query:
        component = find_component(manifest, component_query)
        result["component"] = component
    return result


def resolve(
    root: Path,
    explicit: str | None,
    component_query: str | None,
    latest_explicit: str | None,
) -> dict[str, Any]:
    sources = candidate_sources(root, explicit)
    candidates = [path for path, _ in sources]
    existing = [path for path in candidates if path.is_file()]
    if len(existing) > 1 and not explicit:
        return {
            "status": "ambiguous",
            "targetRoot": str(root),
            "candidates": [str(path) for path in existing],
            "providers": [provider for path, provider in sources if path in existing],
        }
    if not existing:
        return {
            "status": "not-found",
            "targetRoot": str(root),
            "candidates": [str(path) for path in candidates],
        }

    manifest_path = existing[0]
    provider = next(provider for path, provider in sources if path == manifest_path)
    manifest = load_json(manifest_path)
    validate_manifest(manifest, manifest_path)
    result: dict[str, Any] = {
        "status": "found",
        "targetRoot": str(root),
        "provider": provider,
        "manifest": str(manifest_path),
        "systemId": manifest["systemId"],
        "displayName": manifest["displayName"],
        "releaseVersion": manifest["releaseVersion"],
        "contractVersion": manifest["contractVersion"],
        "hosts": manifest["hosts"],
        "packages": manifest.get("packages"),
        "styles": manifest.get("styles"),
        "documentation": manifest.get("documentation"),
        "conformance": manifest.get("conformance"),
        "componentCount": len(manifest["components"]),
    }

    if component_query:
        result["componentQuery"] = component_query
        component = find_component(manifest, component_query)
        if component is None:
            result["status"] = "component-not-found"
        else:
            result["component"] = component

    latest_path = configured_latest_manifest(root, latest_explicit)
    if latest_path is not None:
        if not latest_path.is_file():
            result["latest"] = {
                "status": "not-found",
                "manifest": str(latest_path),
            }
        else:
            result["latest"] = latest_summary(latest_path, component_query)

    if result.get("latest") is None:
        registry_candidates = local_registry_manifests(root)
        if len(registry_candidates) == 1:
            result["latest"] = latest_summary(
                registry_candidates[0], component_query, "local-registry"
            )
        elif len(registry_candidates) > 1:
            result["latest"] = {
                "status": "ambiguous",
                "candidates": [str(path) for path in registry_candidates],
                "provider": "local-registry",
            }

    return result


def render_text(result: dict[str, Any]) -> str:
    lines = [
        f"Catalog status: {result['status']}",
        f"Target root: {result['targetRoot']}",
    ]
    if result["status"] == "ambiguous":
        lines.append("Candidates:")
        lines.extend(f"- {candidate}" for candidate in result["candidates"])
        return "\n".join(lines)
    if result["status"] == "not-found":
        lines.append("Searched:")
        lines.extend(f"- {candidate}" for candidate in result["candidates"])
        return "\n".join(lines)

    lines.extend(
        [
            f"Manifest: {result['manifest']}",
            f"Provider: {result.get('provider', 'manifest')}",
            f"Design System: {result['displayName']} ({result['systemId']})",
            f"Installed release: {result['releaseVersion']}",
            f"Contract version: {result['contractVersion']}",
            f"Hosts: {', '.join(result['hosts'])}",
            f"Components: {result['componentCount']}",
        ]
    )
    if result.get("latest"):
        latest = result["latest"]
        if latest["status"] == "found":
            lines.append(
                f"Latest catalog candidate: {latest['releaseVersion']} "
                f"(contract {latest['contractVersion']})"
            )
            if result.get("componentQuery"):
                latest_component = latest.get("component")
                lines.append(
                    "Latest component: "
                    + (
                        f"{latest_component['displayName']} ({latest_component['id']})"
                        if latest_component
                        else "not found"
                    )
                )
        elif latest["status"] == "ambiguous":
            lines.append("Latest catalog candidate: ambiguous")
        else:
            lines.append(f"Latest catalog candidate: not found ({latest['manifest']})")
    if result.get("styles"):
        styles = result["styles"]
        lines.append(f"Styles: {styles.get('package')} -> {styles.get('entrypoint')}")
    if result.get("component"):
        component = result["component"]
        lines.extend(
            [
                f"Component: {component['displayName']} ({component['id']})",
                f"Status: {component['status']}",
                f"Contract: {component['contract']}",
            ]
        )
        for host, api in component.get("hostApis", {}).items():
            api_name = api.get("export") or api.get("type") or "unlisted"
            lines.append(f"{host.title()} API: {api_name} ({api.get('package', 'package unspecified')})")
    elif result.get("componentQuery"):
        lines.append(f"Component query: {result['componentQuery']}")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target", nargs="?", default=".", help="Target repository root")
    parser.add_argument("--manifest", help="Explicit manifest path")
    parser.add_argument("--latest-manifest", help="Optional local manifest used for update discovery")
    parser.add_argument("--component", help="Component ID, alias, or unique display name")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args()

    root = Path(args.target).expanduser().resolve()
    if not root.is_dir():
        fail(f"target is not a directory: {root}")

    result = resolve(root, args.manifest, args.component, args.latest_manifest)
    if args.format == "json":
        print(json.dumps(result, indent=2))
    else:
        print(render_text(result))
    return 0


if __name__ == "__main__":
    sys.exit(main())
