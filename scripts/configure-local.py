#!/usr/bin/env python3
"""Create an ignored, stable development signing key without printing it."""
import json
import os
from pathlib import Path
import secrets
path = Path(__file__).resolve().parent.parent / 'src/GetYourGigs.Api/appsettings.Local.json'
if not path.exists():
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, 'w') as output:
        json.dump({'Jwt': {'Key': secrets.token_urlsafe(48)}}, output, indent=2)
    print('Created ignored local development configuration.')
