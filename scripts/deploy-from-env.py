#!/usr/bin/env python3
"""Run local deployment with private .env values without putting them in argv."""
import os
from pathlib import Path

root = Path(__file__).resolve().parent.parent
path = root / ".env"
if not path.is_file():
    raise SystemExit("Private .env is missing")
env = os.environ.copy()
for raw in path.read_text().splitlines():
    line = raw.strip()
    if not line or line.startswith("#") or "=" not in line:
        continue
    key, value = line.split("=", 1)
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in ('"', "'"):
        value = value[1:-1]
    env.setdefault(key.strip(), value)
os.execve(str(root / "scripts" / "deploy-local.sh"), ["deploy-local.sh"], env)
