#!/usr/bin/env python3

import os
import sys
import time
import signal

CHUNK_SIZE = 1024 * 1024  # 1 MiB
TMP_NAME = "sfill.tmp"

if len(sys.argv) != 2:
    print(f"Usage: {sys.argv[0]} DIRECTORY")
    sys.exit(1)

directory = sys.argv[1]
tmp_path = os.path.join(directory, TMP_NAME)

start = time.monotonic()
written = 0
last_time = start
last_written = 0
free = 0


def cleanup():
    """Remove the temporary file if it exists."""
    try:
        os.remove(tmp_path)
    except FileNotFoundError:
        pass


def cleanup_and_exit(signum, frame):
    print("\nInterrupted. Removing temporary file...")
    cleanup()
    print("Done.")
    sys.exit(130)


signal.signal(signal.SIGINT, cleanup_and_exit)

try:
    with open(tmp_path, "ab") as f:
        while True:
            stat = os.statvfs(directory)
            free = stat.f_bavail * stat.f_frsize

            if free <= CHUNK_SIZE:
                break

            f.write(os.urandom(CHUNK_SIZE))
            f.flush()

            written += CHUNK_SIZE

            # Calculate speed from the most recent iteration.
            now = time.monotonic()
            interval = now - last_time
            speed = (
                (written - last_written) / interval
                if interval
                else 0
            )

            # Estimate time remaining.
            remaining = max(0, free - CHUNK_SIZE)
            eta = remaining / speed if speed else 0

            print(
                f"\rFree: {free / 1024**3:7.2f} GB | "
                f"ETA: {eta:8.1f}s | "
                f"Speed: {speed / 1024**2:6.1f} MB/s",
                end="",
                flush=True,
            )

            last_time = now
            last_written = written

except KeyboardInterrupt:
    # Normally SIGINT is handled by cleanup_and_exit().
    cleanup_and_exit(None, None)

finally:
    # Always remove the temporary file on normal exit or exceptions.
    cleanup()

elapsed = time.monotonic() - start

print("\nFinished.")
print(f"Remaining space: {free / 1024**3:.2f} GB")
print(f"Elapsed time:    {elapsed:.1f} seconds")
print(f"Data written:    {written / 1024**3:.2f} GB")
