#!/usr/bin/env bash
# Release-safety rehearsal for the WPF version: can a bad release brick users, can they still
# update, and do we get the crash reports? Runs real Squirrel installs and updates in a Windows VM
# (UTM, driven with utmctl) against a local feed, with crash reports going to a Bugsink server in
# Docker on this Mac. Every step is checked; the first failed check stops the run.
#
#   Build/release-safety/test-release-safety.sh
#
# Needs: the UTM VM (VM below) with Visual Studio and git, logged on; this branch cloned in the VM
# at $REPO (the script updates it from a bundle); a Bugsink container (BUGSINK) with a project
# (PROJECT). Uses the test app id YAPA2Test, never a real YAPA2 install; Documents\YAPA2 in the VM
# is backed up first and restored at the end.
set -euo pipefail

VM="${VM:-YAPA Dev}"
REPO='C:\yapa-work\YAPA-2'
BUGSINK="${BUGSINK:-yapa-bugsink}"
PROJECT="${PROJECT:-YAPA 2}"
FEED='C:\yapa-work\release-safety\feed'
A=9.0.1 B=9.0.2 BROKEN=9.0.3 FIX=9.0.4
HERE="$(cd "$(dirname "$0")" && pwd)"
TMP="$(mktemp -d)"

DSN=$(docker exec "$BUGSINK" bugsink-manage shell -c "from projects.models import Project; print(Project.objects.get(name='$PROJECT').dsn)" 2>/dev/null | tail -1)
[ -n "$DSN" ] || { echo "no Bugsink project '$PROJECT' in container $BUGSINK"; exit 1; }

vm() {   # vm <release-safety.ps1 arguments...>: one step in the VM, its output printed
    printf '%s\r\n' '@echo off' "set YAPA_UPDATE_URL=$FEED" "set YAPA_CRASH_DSN=$DSN" \
        "powershell -NoProfile -ExecutionPolicy Bypass -File $REPO\\Build\\release-safety\\release-safety.ps1 $*" \
        | utmctl file push "$VM" 'C:\yapa-work\release-safety-step.cmd'
    utmctl exec "$VM" --cmd cmd.exe /c 'C:\yapa-work\release-safety-step.cmd' 2>&1 | tr -d '\r'
}

STATUS=""
status() { STATUS="$(vm -Action status)"; }
fail() { echo "FAIL: $*"; echo "$STATUS" | sed 's/^/  | /'; exit 1; }
pass() { echo "ok:   $*"; }
has() { grep -q -- "$1" <<<"$STATUS"; }
lifetime() {   # seconds from the last start to the "failed to start ... instead" log line that follows it
    python3 - "$STATUS" <<'PY'
import re, sys, datetime
s = sys.argv[1]
start = re.search(r"last start: (\S+ \S+)", s)
lines = re.findall(r"(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d+) .*failed to start", s)
if not start or not lines:
    print("?"); sys.exit()
t0 = datetime.datetime.fromisoformat(start.group(1))
after = [datetime.datetime.fromisoformat(x) for x in lines if datetime.datetime.fromisoformat(x) >= t0]
print("%.1f" % ((after[0] - t0).total_seconds()) if after else "?")
PY
}
folders() { echo "   on disk: $(awk '/^installed:/{f=1;next} /^running:/{f=0} f' <<<"$STATUS" | xargs)"; }
running_version() { grep -A1 '^running:' <<<"$STATUS" | tail -1 | awk '{print $3}'; }

wait_status() {   # wait_status <seconds> <what> <grep pattern>: until the status shows the pattern
    local seconds=$1 what=$2 pattern=$3
    for _ in $(seq 1 $((seconds / 5))); do
        status
        if has "$pattern"; then return 0; fi
        sleep 5
    done
    fail "timed out waiting for: $what"
}

events() {   # events <release>: this run's crash reports for that release, "type|handled|seconds from crash to arrival|event id|crash time"
    docker exec "$BUGSINK" bugsink-manage shell -c "
import datetime
from events.models import Event
since = datetime.datetime.fromisoformat('$RUN_START').replace(tzinfo=datetime.timezone.utc)
for e in Event.objects.filter(project__name='$PROJECT', timestamp__gte=since).order_by('timestamp'):
    d = e.get_parsed_data()
    if d.get('release') != 'yapa@$1': continue
    x = d['exception']['values'][-1]
    print(x.get('type'), x.get('mechanism', {}).get('handled'), int((e.ingested_at - e.timestamp).total_seconds()), e.event_id, e.timestamp.isoformat()[11:23], sep='|')
" 2>/dev/null | grep -v -e "objects imported" -e '^$' || true
}

RUN_START=$(date -u +%Y-%m-%dT%H:%M:%S)
echo "== preparing (VM: $VM, Bugsink: $BUGSINK)"
(cd "$HERE/../.." && git bundle create "$TMP/branch.bundle" HEAD master --tags >/dev/null 2>&1)
NAME="branch-$(date +%H%M%S).bundle"
utmctl file push "$VM" "C:\\yapa-work\\$NAME" < "$TMP/branch.bundle"
printf '%s\r\n' '@echo off' "cd /d $REPO" "git fetch -q --tags ..\\$NAME HEAD 2>&1" 'git reset -q --hard FETCH_HEAD' "git fetch -q ..\\$NAME master:master 2>&1" 'git log --oneline -1' \
    | utmctl file push "$VM" 'C:\yapa-work\release-safety-step.cmd'
utmctl exec "$VM" --cmd cmd.exe /c 'C:\yapa-work\release-safety-step.cmd' 2>&1 | tr -d '\r'
vm -Action stop >/dev/null
vm -Action clean >/dev/null
vm -Action backup >/dev/null
# scenarios 1-6 check delivery, so reports go without asking; scenario 7 checks the asking
vm -Action crash-mode -Mode Always >/dev/null
trap 'echo "== cleaning up"; vm -Action clean >/dev/null; vm -Action restore; docker start "$BUGSINK" >/dev/null 2>&1 || true' EXIT

echo "== 1. a normal install: $A"
vm -Action build -Version $A
vm -Action install -Version $A >/dev/null
wait_status 90 "Setup to install $A" "app-$A"
vm -Action start >/dev/null
wait_status 60 "$A to run" "pid .* $A.0 "
pass "Setup installed $A, and it starts"
vm -Action stop >/dev/null

echo "== 2. a normal update: $A -> $B"
vm -Action build -Version $B
vm -Action start >/dev/null
wait_status 120 "$A to install $B" "Installed $B"
vm -Action stop >/dev/null
vm -Action start >/dev/null
wait_status 60 "$B to run" "pid .* $B.0 "
pass "$A installed $B, and the next start runs $B"
folders
vm -Action stop >/dev/null

echo "== 3. a bad release: $BROKEN crashes while starting"
vm -Action build -Version $BROKEN -Broken
vm -Action start >/dev/null
wait_status 120 "$B to install $BROKEN" "Installed $BROKEN"
vm -Action stop >/dev/null
status; folders
has "app-$B" || fail "Squirrel removed $B's folder when it installed $BROKEN: nothing to fall back to"
pass "$B's folder is kept after $BROKEN is installed"
echo "   (Bugsink is stopped: the crash happens offline)"
docker stop "$BUGSINK" >/dev/null
vm -Action start >/dev/null
wait_status 90 "the recovery to start $B instead of $BROKEN" "app-$BROKEN failed to start"
sleep 10; status
grep -q "^  pid .* $B.0 .*app-$B" <<<"$STATUS" || fail "the previous version $B is not running"
# quietly: no dialog keeps the failed version alive
grep -q "^  pid .*app-$BROKEN" <<<"$STATUS" && fail "the failed version $BROKEN is still running"
pass "$BROKEN crashed while starting and $B was started in its place"
echo "   from starting $BROKEN to handing over to $B, server unreachable: $(lifetime) s (start launches through Squirrel's stub)"
grep -A1 "crash reports waiting" <<<"$STATUS" | tail -1 | grep -q -v " 0$" || fail "no crash report was kept to send later"
pass "the crash report was kept to send later (server unreachable)"
vm -Action stop >/dev/null
docker start "$BUGSINK" >/dev/null
for _ in $(seq 1 30); do curl -s -o /dev/null "http://192.168.64.1:8000/" && break; sleep 1; done

echo "== 4. the next start: still no way to brick, and the waiting report arrives"
vm -Action start >/dev/null
wait_status 90 "$B to run again" "app-$B"
sleep 25; status
echo "   from starting $BROKEN to handing over to $B, server up: $(lifetime) s"
has "Updating from" || fail "the fallback version did not look for updates"
grep -q "Installed $BROKEN" <<<"$(grep -A20 '^log:' <<<"$STATUS" | tail -6)" && fail "the fallback version installed the broken release again"
REPORTS="$(events $BROKEN)"
echo "$REPORTS" | sed 's/^/   report (type|handled|seconds to arrive|event id|crash time): /'
DUPLICATES=$(cut -d'|' -f5 <<<"$REPORTS" | sort | uniq -d | wc -l | tr -d ' ')
echo "   crashes reported more than once: $DUPLICATES (same crash time = one crash)"
grep -q "^System.IO.FileNotFoundException|False|" <<<"$REPORTS" || fail "no unhandled FileNotFoundException report for $BROKEN"
# the offline crash's report was saved and sent at a later start: it arrives well after it was made
[ "$(awk -F'|' '$3 >= 20' <<<"$REPORTS" | wc -l | tr -d ' ')" -ge 1 ] || fail "the report from the offline crash never arrived"
pass "Bugsink has the $BROKEN start crash, including the one from while it was offline"
echo "   $B's update check with $BROKEN the newest on disk:"
grep -E 'Updating from|Up to date|Installed' <<<"$STATUS" | tail -2 | cut -c1-140 | sed 's/^ */     /'
folders
vm -Action stop >/dev/null

echo "== 5. the fix: $FIX reaches users who are on the fallback version"
vm -Action build -Version $FIX
vm -Action start >/dev/null
wait_status 150 "the fallback $B to install $FIX" "Installed $FIX"
status; echo "   after $FIX was installed, with $B running:"; folders
vm -Action stop >/dev/null
vm -Action start >/dev/null
wait_status 60 "$FIX to run" "pid .* $FIX.0 "
sleep 5; status
has "pid .* $FIX.0 " || fail "$FIX did not keep running"
pass "the fallback installed $FIX, and $FIX starts normally"
folders
echo "   a command launch (/stop) while $FIX runs:"
vm -Action measure | sed 's/^/     /'
vm -Action stop >/dev/null

echo "== 6. a crash while in use is reported with its version"
vm -Action start -Arguments /crashtest >/dev/null
for _ in $(seq 1 30); do REPORTS="$(events $FIX)"; [ -n "$REPORTS" ] && break; sleep 2; done
grep -q "^System.InvalidOperationException|False|" <<<"$REPORTS" || fail "no unhandled crash report for $FIX"
pass "the crash in $FIX reached Bugsink, marked unhandled"
vm -Action stop >/dev/null

echo "== 7. \"Ask before sending\" (the default): the user sees the report and decides"
vm -Action crash-mode -Mode Ask >/dev/null
COUNT_BEFORE=$(events $FIX | wc -l | tr -d ' ')
vm -Action start -Arguments /crashtest >/dev/null
wait_status 60 "the crash to be saved" "waiting to be sent:.*" ; sleep 15
vm -Action stop >/dev/null
status; grep -A1 "crash reports waiting" <<<"$STATUS" | tail -1 | grep -q " 1$" || fail "the crash was not saved for asking"
[ "$(events $FIX | wc -l | tr -d ' ')" = "$COUNT_BEFORE" ] || fail "a report was sent before the user was asked"
pass "the crash was saved, and nothing was sent before asking"
vm -Action start >/dev/null
ANSWER="$(vm -Action answer -Choice DontSend)"
grep -q "^WINDOW" <<<"$ANSWER" || { STATUS="$ANSWER"; fail "no crash report window at the next start"; }
echo "$ANSWER" | sed -n '2,40p' | sed 's/^/   window: /'
grep -q "InvalidOperationException" <<<"$ANSWER" || fail "the window does not show the error"
grep -q "YAPA $FIX" <<<"$ANSWER" || fail "the window does not show the version"
grep -q -i "Dev" <<<"$(grep -v -i "device" <<<"$ANSWER")" && fail "the window shows the Windows user name"
sleep 5; status
grep -A1 "crash reports waiting" <<<"$STATUS" | tail -1 | grep -q " 0$" || fail "the declined report was not removed"
[ "$(events $FIX | wc -l | tr -d ' ')" = "$COUNT_BEFORE" ] || fail "the declined report was sent"
pass "the window showed what would be sent; \"Don't send\" sent nothing and removed it"
vm -Action stop >/dev/null
vm -Action start -Arguments /crashtest >/dev/null
sleep 20; vm -Action stop >/dev/null
vm -Action start >/dev/null
ANSWER="$(vm -Action answer -Choice Send -Remember)"
grep -q "^WINDOW" <<<"$ANSWER" || { STATUS="$ANSWER"; fail "no crash report window after the second crash"; }
for _ in $(seq 1 20); do [ "$(events $FIX | wc -l | tr -d ' ')" -gt "$COUNT_BEFORE" ] && break; sleep 2; done
[ "$(events $FIX | wc -l | tr -d ' ')" -gt "$COUNT_BEFORE" ] || fail "\"Send\" did not send the report"
status; has "Always" || fail "\"Remember my choice\" did not switch to sending without asking"
pass "\"Send\" sent it, and \"Remember my choice\" switched to sending without asking"
vm -Action stop >/dev/null

echo "PASS: a bad release did not brick YAPA, users could update past it, and every crash was reported"
