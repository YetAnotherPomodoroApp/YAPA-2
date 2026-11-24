#!/usr/bin/env bash
# End-to-end test of the Avalonia version's updater on Linux, from a Mac or Linux machine with
# Docker: packs two AppImage releases, then in a Debian container (virtual X display) runs the
# same checks as test-updates-macos.sh: download while running, nothing installed under a
# running app, install on a normal close, install at the next start after a kill, and an
# unreachable feed only logged.
#
#   Build/test-updates-linux.sh [work folder]
#
# Needs Docker and the vpk tool (dotnet tool install -g vpk).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="${1:-$(mktemp -d -t yapa-update-test-linux)}"
OLD=2.1.1
NEW=2.1.2
ARCH=$(docker info --format '{{.Architecture}}')
case "$ARCH" in aarch64|arm64) RID=linux-arm64 ;; *) RID=linux-x64 ;; esac
VPK="${VPK:-$(command -v vpk || echo "$HOME/.dotnet/tools/vpk")}"
export DOTNET_ROLL_FORWARD=Major

echo "work folder: $WORK"
rm -rf "$WORK/releases" "$WORK/publish" "$WORK/old"
mkdir -p "$WORK/releases" "$WORK/old"

pack() {
    local version=$1
    echo "== building $version for $RID"
    dotnet publish "$ROOT/YAPA.Avalonia/YAPA.Avalonia.csproj" -c Release -r "$RID" --self-contained \
        -p:Version="$version" -o "$WORK/publish/$version" -v quiet -nologo >/dev/null
    "$VPK" "[linux]" pack -u YAPA2 -v "$version" -r "$RID" -p "$WORK/publish/$version" -e YAPA \
        --packTitle "YAPA 2" --icon "$ROOT/YAPA/Resources/Pomodoro.png" -o "$WORK/releases" --yes >/dev/null
}

pack "$OLD"
cp "$WORK/releases/"*.AppImage "$WORK/old/YAPA2.AppImage"
pack "$NEW"
ls "$WORK/releases" | sed 's/^/  release file: /'

cat > "$WORK/in-container.sh" <<'EOF'
set -euo pipefail
OLD=$1; NEW=$2
apt-get update -qq >/dev/null
apt-get install -y -qq xvfb libx11-6 libice6 libsm6 libfontconfig1 libxrandr2 libxi6 libxcursor1 libxext6 libxrender1 libssl3 ca-certificates >/dev/null 2>&1
Xvfb :99 -screen 0 1280x800x24 >/dev/null 2>&1 &
export DISPLAY=:99
export APPIMAGE_EXTRACT_AND_RUN=1   # no FUSE in a container
export YAPA_HOME=/tmp/yapa-home
LOG=$YAPA_HOME/update.log
cp /work/old/YAPA2.AppImage /tmp/YAPA2.AppImage
chmod +x /tmp/YAPA2.AppImage
apt-get install -y -qq procps >/dev/null 2>&1

fail() {
    echo "FAIL: $*"
    [ -f "$LOG" ] && sed 's/^/  log: /' "$LOG"
    sed 's/^/  out: /' /tmp/yapa.out 2>/dev/null | tail -20
    ps -eo pid,args | grep -v -E "ps -eo|grep" | sed 's/^/  ps: /' | tail -15
    exit 1
}
pass() { echo "ok:   $*"; }
wait_for() { local n=$1 what=$2; shift 2; for _ in $(seq 1 "$n"); do "$@" && return 0; sleep 1; done; fail "timed out waiting for: $what"; }
logged() { grep -q "$1" "$LOG" 2>/dev/null; }
running() { pgrep -x YAPA >/dev/null; }
stopped() { ! running; }
sum() { md5sum "$APP" | cut -d' ' -f1; }
run() { local feed=$1; shift; YAPA_UPDATE_URL="$feed" "$APP" "$@" >/tmp/yapa.out 2>&1 & }
close_app() { "$APP" /exit >/dev/null 2>&1 || true; wait_for 20 "YAPA to close" stopped; }
kill_app() { pkill -9 -x YAPA || true; wait_for 10 "YAPA to die" stopped; }

APP=/tmp/YAPA2.AppImage
OLD_SUM=$(sum)
echo "== starting $OLD and leaving it open"
run /work/releases
wait_for 90 "the old version to download $NEW" logged "downloaded $NEW"
logged "running $OLD" || fail "the log does not say it runs $OLD"
pass "$OLD found and downloaded $NEW"
echo "   (waiting 75 s: Velopack's helper gives up waiting for the app after a minute)"
sleep 75
running || fail "the app stopped while it had an update waiting"
[ "$(sum)" = "$OLD_SUM" ] || fail "the update was installed while the app was still running"
pass "$OLD keeps running untouched while the update waits"

close_app
wait_for 60 "the AppImage to be replaced" test "$(sum)" != "$OLD_SUM"
logged "closing, installing $NEW" || fail "the log does not say the update was installed at exit"
pass "closing $OLD replaced the AppImage with $NEW"

echo "== starting $NEW"
run /work/releases
wait_for 60 "$NEW to start and check" logged "running $NEW"
wait_for 30 "$NEW to find no newer version" logged "up to date"
close_app
pass "$NEW starts and finds itself up to date"

echo "== starting $NEW with no network (a feed address nothing answers at)"
run http://127.0.0.1:9/releases
wait_for 30 "the failure to be logged" logged "update failed"
sleep 2
running || fail "the app stopped when the feed was unreachable"
close_app
pass "an unreachable feed is logged and the app keeps running"

echo "== an old version that is killed instead of closed"
rm -rf /var/tmp/velopack/YAPA2
export YAPA_HOME=/tmp/yapa-home-killed
LOG=$YAPA_HOME/update.log
APP=/tmp/YAPA2-killed.AppImage
cp /work/old/YAPA2.AppImage "$APP"
chmod +x "$APP"
run /work/releases
wait_for 90 "the old version to download $NEW" logged "downloaded $NEW"
kill_app
[ "$(sum)" = "$OLD_SUM" ] || fail "a killed app was changed"
run /work/releases
wait_for 90 "the next start to be $NEW" logged "running $NEW"
close_app
pass "a killed $OLD gets $NEW at its next start"
echo "PASS: updates work on Linux"
EOF

docker run --rm -v "$WORK:/work" debian:bookworm-slim bash /work/in-container.sh "$OLD" "$NEW"
