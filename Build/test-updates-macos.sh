#!/usr/bin/env bash
# End-to-end test of the Avalonia version's updater on macOS, all local:
# packs two versions into a releases folder, installs the older one and checks that
#   - it finds and downloads the newer one, and leaves itself alone while it keeps running
#     (also past the minute Velopack's helper would wait),
#   - closing it normally installs the update in place,
#   - an app that was killed instead gets the update at its next start,
#   - the new version finds itself up to date, and an unreachable feed is only logged.
#
#   Build/test-updates-macos.sh [work folder]
#
# Needs the vpk tool: dotnet tool install -g vpk
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="${1:-$(mktemp -d -t yapa-update-test)}"
OLD=2.1.1
NEW=2.1.2
RID=osx-$( [ "$(uname -m)" = arm64 ] && echo arm64 || echo x64 )
VPK="${VPK:-$(command -v vpk || echo "$HOME/.dotnet/tools/vpk")}"
export DOTNET_ROLL_FORWARD=Major   # vpk runs on whatever .NET is installed

echo "work folder: $WORK"
# Velopack keeps downloaded packages per app id, outside the app; start from none
rm -rf "$HOME/Library/Caches/velopack/YAPA2"
rm -rf "$WORK/releases" "$WORK/publish" "$WORK/installed" "$WORK/killed" "$WORK/home" "$WORK/home-killed"
mkdir -p "$WORK/releases" "$WORK/installed" "$WORK/killed"

HOME_DIR="$WORK/home"
fail() { echo "FAIL: $*"; [ -f "$HOME_DIR/update.log" ] && sed 's/^/  log: /' "$HOME_DIR/update.log"; exit 1; }
pass() { echo "ok:   $*"; }

# the app icon, from the WPF app's picture
ICON="$WORK/YAPA.icns"
[ -f "$ICON" ] || sips -s format icns "$ROOT/YAPA/Resources/Pomodoro.png" --out "$ICON" >/dev/null

pack() {
    local version=$1
    echo "== building $version"
    dotnet publish "$ROOT/YAPA.Avalonia/YAPA.Avalonia.csproj" -c Release -r "$RID" --self-contained \
        -p:Version="$version" -o "$WORK/publish/$version" -v quiet -nologo >/dev/null
    "$VPK" pack -u YAPA2 -v "$version" -p "$WORK/publish/$version" -e YAPA --packTitle "YAPA 2" \
        --icon "$ICON" -o "$WORK/releases" --yes >/dev/null
}

wait_for() {   # wait_for <seconds> <description> <command...>
    local seconds=$1 what=$2; shift 2
    for _ in $(seq 1 "$seconds"); do
        if "$@"; then return 0; fi
        sleep 1
    done
    fail "timed out waiting for: $what"
}

# the bundle may say 2.1.1.0 for 2.1.1
bundle_version() { /usr/libexec/PlistBuddy -c "Print :CFBundleShortVersionString" "$APP/Contents/Info.plist" | sed -E 's/^([0-9]+\.[0-9]+\.[0-9]+)\.0$/\1/'; }
logged() { grep -q "$1" "$HOME_DIR/update.log" 2>/dev/null; }
count() { grep -c "$1" "$HOME_DIR/update.log" 2>/dev/null || true; }
running() { pgrep -f "$APP/Contents/MacOS/YAPA" >/dev/null; }
stopped() { ! running; }

run_app() {   # run_app <feed> [args]: start YAPA with its own data folder and the given update feed
    local feed=$1; shift
    YAPA_HOME="$HOME_DIR" YAPA_UPDATE_URL="$feed" "$APP/Contents/MacOS/YAPA" "$@" >/dev/null 2>&1 &
}

close_app() {   # as the user does: a normal exit, via YAPA's /exit command
    YAPA_HOME="$HOME_DIR" "$APP/Contents/MacOS/YAPA" /exit >/dev/null 2>&1 || true
    wait_for 20 "YAPA to close" stopped
}

kill_app() { pkill -9 -f "$APP/Contents/MacOS/YAPA" || true; wait_for 10 "YAPA to die" stopped; }
trap 'pkill -9 -f "$WORK/.*/YAPA" 2>/dev/null || true' EXIT

pack "$OLD"
cp "$WORK/releases/YAPA2-osx-Portable.zip" "$WORK/old-portable.zip"
pack "$NEW"
ls "$WORK/releases" | sed 's/^/  release file: /'

# install the old version, as a user who downloaded it before the new one came out
ditto -x -k "$WORK/old-portable.zip" "$WORK/installed"
APP="$(find "$WORK/installed" -maxdepth 1 -name '*.app' | head -1)"
[ -n "$APP" ] || fail "no .app in the portable zip"
[ "$(bundle_version)" = "$OLD" ] || fail "installed app is $(bundle_version), expected $OLD"
pass "installed $OLD at $APP"

echo "== starting $OLD and leaving it open"
run_app "$WORK/releases"
wait_for 60 "the old version to download $NEW" logged "downloaded $NEW"
logged "running $OLD" || fail "the log does not say it runs $OLD"
pass "$OLD found and downloaded $NEW"
echo "   (waiting 75 s: Velopack's helper gives up waiting for the app after a minute)"
sleep 75
running || fail "the app stopped while it had an update waiting"
[ "$(bundle_version)" = "$OLD" ] || fail "the update was installed while the app was still running"
pass "$OLD keeps running untouched while the update waits"

close_app
wait_for 60 "the app on disk to become $NEW" test "$(bundle_version)" = "$NEW"
logged "closing, installing $NEW" || fail "the log does not say the update was installed at exit"
pass "closing $OLD installed $NEW in place"

echo "== starting $NEW"
run_app "$WORK/releases"
wait_for 30 "$NEW to start and check" logged "running $NEW"
wait_for 30 "$NEW to find no newer version" logged "up to date"
close_app
pass "$NEW starts and finds itself up to date"

echo "== starting $NEW with no network (a feed address nothing answers at)"
run_app "http://127.0.0.1:9/releases"
wait_for 30 "the failure to be logged" logged "update failed"
sleep 2
running || fail "the app stopped when the feed was unreachable"
close_app
pass "an unreachable feed is logged and the app keeps running"

echo "== an old version that is killed instead of closed"
rm -rf "$HOME/Library/Caches/velopack/YAPA2"
HOME_DIR="$WORK/home-killed"
ditto -x -k "$WORK/old-portable.zip" "$WORK/killed"
APP="$(find "$WORK/killed" -maxdepth 1 -name '*.app' | head -1)"
run_app "$WORK/releases"
wait_for 60 "the old version to download $NEW" logged "downloaded $NEW"
kill_app
[ "$(bundle_version)" = "$OLD" ] || fail "a killed app was changed"
run_app "$WORK/releases"
wait_for 60 "the next start to be $NEW" logged "running $NEW"
[ "$(bundle_version)" = "$NEW" ] || fail "the app on disk is still $(bundle_version)"
close_app
pass "a killed $OLD gets $NEW at its next start"

echo "PASS: updates work on macOS ($RID)"
