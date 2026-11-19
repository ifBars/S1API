"""Run the assembly comparison and check for an exact, owner-approved exception."""

import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

IFBARS_ID = 114284668


def approved(pages, command):
    return any(
        comment["user"]["id"] == IFBARS_ID and comment["body"].strip() == command
        for page in pages
        for comment in page
    )


def main():
    checker = Path(os.environ["RUNNER_TEMP"]) / "source-compatibility-tool" / "S1APISourceCompatibility.dll"
    result = subprocess.run(["dotnet", checker, *sys.argv[1:]], stdout=subprocess.PIPE)
    if result.returncode == 0:
        return 0
    if result.returncode != 2 or not result.stdout.strip():
        sys.exit("Source compatibility comparison could not complete.")

    report = result.stdout.decode("utf-8")
    print(report)
    digest = hashlib.sha256(result.stdout).hexdigest()
    command = f"/approve-source-break {os.environ['HEAD_SHA']} {digest}"
    endpoint = f"repos/{os.environ['GITHUB_REPOSITORY']}/issues/{os.environ['PR_NUMBER']}/comments?per_page=100"
    pages = json.loads(subprocess.check_output(["gh", "api", "--paginate", "--slurp", endpoint]))
    if approved(pages, command):
        print("ifBars approved the reported changes for this PR head.")
        return 0

    with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as summary:
        summary.write("### Source compatibility review required\n\n")
        summary.write("To accept these changes, ifBars can post this command as a PR comment and rerun the failed job.\n\n")
        summary.write(f"`{command}`\n\n")
        for line in report.splitlines():
            summary.write(f"    {line}\n")
    return 1


if __name__ == "__main__":
    sys.exit(main())
