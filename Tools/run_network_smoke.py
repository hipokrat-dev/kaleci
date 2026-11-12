#!/usr/bin/env python3
"""Run two development players over loopback or the linked project's Relay.

Uses synthetic player names. Relay creates a temporary two-player private session.
Logs stay local; neither anonymous credentials nor room codes are published.
"""
import argparse
from pathlib import Path
import re
import subprocess
import time


def run(relay, motion=False):
    root = Path(__file__).resolve().parents[1]
    executable = root / "Builds/PenaltiArena.app/Contents/MacOS/Penaltı Arena"
    if not executable.exists():
        candidates = list((root / "Builds/PenaltiArena.app/Contents/MacOS").iterdir())
        if len(candidates) != 1:
            raise RuntimeError("Build the macOS development player first")
        executable = candidates[0]
    mode = ("relay" if relay else "local") + ("-motion" if motion else "")
    host_log = root / f"Logs/{mode}-host.log"
    client_log = root / f"Logs/{mode}-client.log"
    for log in (host_log, client_log):
        log.unlink(missing_ok=True)
    processes = []
    def launch(log, flags):
        if motion:
            flags = [*flags, "-arena-motion-test"]
        process = subprocess.Popen([str(executable), "-batchmode", "-nographics", "-logFile", str(log), *flags], cwd=root, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
        processes.append(process)
        return process
    try:
        host = launch(host_log, ["-arena-relay-host" if relay else "-arena-smoke-host"])
        deadline = time.monotonic() + 230
        if relay:
            code = None
            while time.monotonic() < deadline and host.poll() is None:
                match = re.search(r"SMOKE_RELAY_CODE=(\w+)", host_log.read_text(errors="replace") if host_log.exists() else "")
                if match:
                    code = match[1]
                    break
                time.sleep(.5)
            if not code:
                raise RuntimeError("Relay host did not create a room; inspect the local host log")
            print("Relay room created; starting second anonymous profile.", flush=True)
            client = launch(client_log, ["-arena-relay-client", code])
        else:
            time.sleep(2)
            client = launch(client_log, ["-arena-smoke-client"])
        while time.monotonic() < deadline and any(p.poll() is None for p in processes):
            time.sleep(.5)
        for name, process, log in (("host", host, host_log), ("client", client, client_log)):
            content = log.read_text(errors="replace")
            expected = "SMOKE_RESULT score=1:0 history=GKKKK/DDDDD winner=0 shots=10" if motion else "SMOKE_RESULT score=5:0 history=GGGGG/DDDDD winner=0 shots=10"
            match = re.search(expected, content)
            if "CHOICE_FLOW_OK" not in content:
                raise RuntimeError(f"{mode} {name} selection flow failed; see {log.name}")
            if motion and "MOTION_PHYSICS_OK" not in content:
                raise RuntimeError(f"{mode} {name} physics failed; see {log.name}")
            if process.poll() != 0 or not match or "SMOKE_BAD_RESULT" in content or "SMOKE_TIMEOUT" in content:
                raise RuntimeError(f"{mode} {name} failed; see {log.name}")
            print(f"{mode} {name}: {match[0]}", flush=True)
    finally:
        for process in processes:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--relay", action="store_true")
    parser.add_argument("--motion", action="store_true")
    args = parser.parse_args()
    run(args.relay, args.motion)
