#!/bin/bash
# Cloud pre-check (docs/feuille-de-route-v3.md "Compiler dans le cloud"): cloudbuild.py, then the NUnit tests under mono.
# Usage: bash tools/cloud/cloudtest.sh [nunit options, e.g. --where "class =~ Alarm"]
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
python3 tools/cloud/cloudbuild.py
# the generator writes CRLF: git sees a change of line endings only; restore the files when the content is the same
if git diff --quiet -- Elite/PropertyInspector/catalog.js Elite/PropertyInspector/commands.js; then
  git checkout -- Elite/PropertyInspector/catalog.js Elite/PropertyInspector/commands.js 2>/dev/null || true
else
  echo "!! catalog.js / commands.js changed (content):"; git diff --stat -- Elite/PropertyInspector/
fi
cd Elite.Tests/bin/Debug
# ManifestTests and InspectorFieldsTests build the plugin path with Windows separators: a link with that literal name
ln -sfn "$ROOT/Elite/bin/Debug/com.mhwlng.elite.sdPlugin" '..\..\..\Elite\bin\Debug\com.mhwlng.elite.sdPlugin'
RUNNER=$(ls -d "$ROOT"/packages/NUnit.ConsoleRunner.*/tools/nunit3-console.exe | sort | tail -1)
mono "$RUNNER" Elite.Tests.dll --work=. --result=TestResult.xml --noheader "$@" 2>&1 | sed -n '/Errors, Failures/,$p;/Test Run Summary/,/Duration/p'
