"""Compare deterministic behavior with the specified delivered MOD, without launching a game.

Requires Windows, Python (standard library only), and the Framework C# compiler.
The two test processes allocate only their own memory. No sockets or game input.
"""
import argparse
import json
import os
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROBES = ROOT / 'tests/parity'


def trace(label, executable, output, compiler, current):
    directory = output / label
    directory.mkdir(parents=True, exist_ok=True)
    target = directory / 'TypeGameWASD.exe'
    shutil.copy2(executable, target)
    driver = directory / 'TypeGameMod.Tests.exe'
    command = [str(compiler), '/nologo', '/target:exe', '/platform:x86',
               '/out:' + str(driver), '/r:' + str(target)]
    if current:
        command.append('/define:CURRENT')
    command += [str(PROBES / 'BehaviorProbe.cs'), str(PROBES / 'BaselineAnimation.cs')]
    subprocess.run(command, check=True)
    result = subprocess.run([str(driver)], capture_output=True, text=True)
    (output / (label + '-trace.txt')).write_text(result.stdout, encoding='utf-8')
    (output / (label + '-stderr.txt')).write_text(result.stderr, encoding='utf-8')
    result.check_returncode()
    return {parts[0]: {'bytes': int(parts[1]), 'sha256': parts[2]}
            for parts in (line.split() for line in result.stdout.splitlines())}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', required=True, type=Path, help='delivered TypeGameWASD.exe before the refactor')
    parser.add_argument('--current', type=Path, default=ROOT / 'artifacts/bin/Release/TypeGameWASD.exe')
    parser.add_argument('--output', type=Path, default=ROOT / 'artifacts/refactor-parity')
    args = parser.parse_args()
    output = args.output.resolve()
    compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
    baseline = trace('baseline', args.baseline.resolve(), output, compiler, False)
    current = trace('current', args.current.resolve(), output, compiler, True)
    report = {name: {'equal': baseline[name] == current[name],
                     'baseline': baseline[name], 'current': current[name]} for name in baseline}
    (output / 'parity.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    for name, result in report.items():
        print(('PASS ' if result['equal'] else 'FAIL ') + name)
    if not all(result['equal'] for result in report.values()):
        raise SystemExit('Behavior changed: inspect ' + str(output / 'parity.json'))


if __name__ == '__main__':
    main()
