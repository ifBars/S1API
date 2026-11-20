"""Compile real consumer-facing metadata for changes that binary ApiCompat can miss."""

from pathlib import Path
import subprocess
import sys
import tempfile


def main(tool):
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        project = root / "Library.csproj"
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
        source = root / "Contract.cs"

        def build(name, code):
            source.write_text(code)
            out = root / name
            subprocess.run(["dotnet", "build", project, "-o", out, "-v:q"], check=True)
            return out / "Library.dll"

        base = """
#nullable enable
public class Contract<T> where T : class? {
    public void Accept<U>() where U : class? {}
    public string Property => "";
#nullable disable
    public void Input(string value) {}
}
#nullable enable
public class Names : System.Collections.Generic.List<string> {}
"""
        baseline = build("before", base)
        current = build("after", base.replace("class?", "class").replace("#nullable disable", "#nullable enable")
                        .replace("string Property", "string? Property").replace("List<string>", "List<string?>"))
        result = subprocess.run(["dotnet", tool, baseline, current], capture_output=True, text=True)
        assert result.returncode == 2, result
        for member in ["T:Contract`1", "Accept``1", "get_Property", "Input(System.String)", "T:Names ->"]:
            assert member in result.stdout, (member, result.stdout)
        leak = build("leak", base + "public class Api { public ScheduleOne.Item[] Items => null!; } namespace ScheduleOne { public class Item {} }")
        result = subprocess.run(["dotnet", tool, baseline, leak], capture_output=True, text=True)
        assert result.returncode == 3 and "get_Items" in result.stdout, result


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
