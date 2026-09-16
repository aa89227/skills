from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parents[1] / "scripts"
REFERENCE_DIR = Path(__file__).resolve().parents[1] / "references"
sys.path.insert(0, str(SCRIPT_DIR))

from inspect_design_system_manifest import resolve as resolve_catalog  # noqa: E402
from resolve_design_system_context import resolve_context  # noqa: E402


EXAMPLE_MANIFEST = REFERENCE_DIR / "design-system.manifest.example.json"


def write_json(path: Path, payload: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2))


class DesignSystemScriptTests(unittest.TestCase):
    def test_consumer_context_resolves_alias_and_host_mapping(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_json(
                root / ".design-system" / "project.json",
                {
                    "role": "consumer",
                    "designSystemId": "company",
                    "hosts": ["react"],
                    "packages": {"react": "@company/design-system-react"},
                },
            )

            context = resolve_context(
                root,
                "consume",
                "react",
                "Modal",
                str(EXAMPLE_MANIFEST),
                str(EXAMPLE_MANIFEST),
            )

            self.assertEqual(context["project"]["role"], "consumer")
            self.assertEqual(context["catalog"]["status"], "found")
            self.assertEqual(context["catalog"]["component"]["id"], "dialog")
            self.assertEqual(context["host"]["selected"], "react")
            self.assertEqual(context["decision"]["outcome"], "resolved-in-consumer")
            self.assertEqual(context["catalog"]["updateStatus"], "current")

    def test_missing_catalog_blocks_consumer_usage(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_json(
                root / ".design-system" / "project.json",
                {
                    "role": "consumer",
                    "designSystemId": "company",
                    "hosts": ["blazor"],
                    "packages": {"blazor": "Company.DesignSystem.Blazor"},
                },
            )

            context = resolve_context(root, "consume", "blazor", "dialog", None, None)

            self.assertEqual(context["catalog"]["status"], "not-found")
            self.assertEqual(context["decision"]["outcome"], "blocked-on-catalog")
            self.assertFalse(context["decision"]["canProceed"])

    def test_invalid_manifest_becomes_normalized_preflight_error(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_json(
                root / ".design-system" / "project.json",
                {
                    "role": "consumer",
                    "designSystemId": "company",
                    "hosts": ["react"],
                    "packages": {"react": "@company/design-system-react"},
                },
            )
            invalid_manifest = root / "design-system.manifest.json"
            invalid_manifest.write_text("{\"schemaVersion\":")

            context = resolve_context(root, "consume", "react", None, str(invalid_manifest), None)

            self.assertEqual(context["catalog"]["status"], "invalid")
            self.assertEqual(context["decision"]["outcome"], "blocked-on-catalog")

    def test_nuget_assets_provider_finds_bundled_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            nuget_root = root / "nuget-cache"
            package_root = nuget_root / "company.designsystem.blazor" / "4.2.0"
            package_root.mkdir(parents=True)
            (package_root / "design-system.manifest.json").write_text(EXAMPLE_MANIFEST.read_text())
            write_json(
                root / "obj" / "project.assets.json",
                {
                    "packageFolders": {f"{nuget_root}/": {}},
                    "libraries": {
                        "Company.DesignSystem.Blazor/4.2.0": {
                            "type": "package",
                            "path": "company.designsystem.blazor/4.2.0",
                        }
                    },
                },
            )

            result = resolve_catalog(root, None, "dialog", None)

            self.assertEqual(result["status"], "found")
            self.assertEqual(result["provider"], "nuget")
            self.assertEqual(result["component"]["id"], "dialog")

    def test_npm_provider_finds_scoped_package_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_json(
                root / ".design-system" / "project.json",
                {
                    "role": "consumer",
                    "designSystemId": "company",
                    "hosts": ["react"],
                    "packages": {"react": "@company/design-system-react"},
                },
            )
            package_manifest = (
                root
                / "node_modules"
                / "@company"
                / "design-system-contracts"
                / "design-system.manifest.json"
            )
            package_manifest.parent.mkdir(parents=True)
            package_manifest.write_text(EXAMPLE_MANIFEST.read_text())

            result = resolve_catalog(root, None, "dialog", None)

            self.assertEqual(result["status"], "found")
            self.assertEqual(result["provider"], "npm")
            self.assertEqual(result["component"]["id"], "dialog")

    def test_component_only_in_latest_catalog_requires_upgrade(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_json(
                root / ".design-system" / "project.json",
                {
                    "role": "consumer",
                    "designSystemId": "company",
                    "hosts": ["react"],
                    "packages": {"react": "@company/design-system-react"},
                },
            )
            installed = root / "installed.json"
            latest = root / "latest.json"
            installed.write_text(EXAMPLE_MANIFEST.read_text())
            latest_payload = json.loads(EXAMPLE_MANIFEST.read_text())
            latest_payload["releaseVersion"] = "4.3.0"
            latest_payload["components"].append(
                {
                    "id": "popover",
                    "displayName": "Popover",
                    "status": "stable",
                    "contract": "components/popover/contract.json",
                    "hostApis": {
                        "react": {
                            "package": "@company/design-system-react",
                            "export": "Popover",
                        }
                    },
                }
            )
            write_json(latest, latest_payload)

            context = resolve_context(
                root,
                "consume",
                "react",
                "popover",
                str(installed),
                str(latest),
            )

            self.assertEqual(context["catalog"]["updateStatus"], "candidate-available")
            self.assertEqual(context["decision"]["outcome"], "blocked-on-package-upgrade")
            self.assertFalse(context["decision"]["canProceed"])

    def test_catalog_for_a_different_design_system_blocks_usage(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_json(
                root / ".design-system" / "project.json",
                {
                    "role": "consumer",
                    "designSystemId": "other-system",
                    "hosts": ["react"],
                    "packages": {"react": "@other/design-system-react"},
                },
            )

            context = resolve_context(
                root,
                "consume",
                "react",
                "dialog",
                str(EXAMPLE_MANIFEST),
                None,
            )

            self.assertEqual(context["decision"]["outcome"], "blocked-on-context")
            self.assertFalse(context["decision"]["canProceed"])


if __name__ == "__main__":
    unittest.main()
