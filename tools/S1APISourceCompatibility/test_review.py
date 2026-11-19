"""Exercise the policy against an isolated Git history, including IL2CPP-only source."""

from pathlib import Path
import json
import subprocess
import sys
import tempfile


def main(tool):
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)

        def git(*args):
            subprocess.run(["git", "-c", "user.name=CI", "-c", "user.email=ci@example.invalid", *args], cwd=root, check=True, capture_output=True)

        git("init")
        folder = root / "S1API"
        folder.mkdir()
        source = folder / "Fixture.cs"
        source.write_text("class Existing { void Load() { Logger.Msg(1); } }")
        git("add", ".")
        git("commit", "-m", "base")
        with source.open("a") as output:
            output.write("""
#if IL2CPPMELON
class Probe : UnityEngine.MonoBehaviour {
    public void Update() { MelonLogger.Warning("repeated"); }
    void Load() { Logger.Debug("diagnostic"); Logger.Warning("once per load"); }
    void Register() { Logger.Msg("registered"); }
    void LogOnce() { if (first) MelonLogger.Error("failed"); }
}
class Loader : MelonPlugin {
    public override void OnApplicationEarlyStart() {
        MelonLogger.Msg(1);
        MelonLogger.Msg(2);
        MelonLogger.Msg(3);
        MelonLogger.Msg(4);
    }
}
#endif
""")
        git("add", ".")
        git("commit", "-m", "candidate")
        report = root / "review.json"
        result = subprocess.run(["dotnet", tool, "--review", "HEAD^", "HEAD", report], cwd=root)
        rules = [item["rule"] for item in json.loads(report.read_text())]
        assert result.returncode == 1 and rules.count("frame-log") == rules.count("startup-budget") == 1, rules
        assert rules.count("console-info") == 4 and rules.count("diagnostic") == 2, rules
        subprocess.run(["dotnet", tool, "--review", "HEAD", "HEAD", report], cwd=root, check=True)
        assert json.loads(report.read_text()) == []


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
