# Generates the mod sounds (WAV 44.1 kHz, 16-bit, stereo) into src\KingdomAccess.Core\Sounds.
# Any sound can be replaced by a WAV file with the same name.
param([string]$OutDir = "$PSScriptRoot\..\src\KingdomAccess.Core\Sounds")

$rate = 44100
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Write-Wav([string]$path, [double[]]$left, [double[]]$right) {
    $n = $left.Length
    $ms = New-Object IO.MemoryStream
    $w = New-Object IO.BinaryWriter($ms)
    $dataLen = $n * 4
    $w.Write([Text.Encoding]::ASCII.GetBytes("RIFF")); $w.Write([int](36 + $dataLen))
    $w.Write([Text.Encoding]::ASCII.GetBytes("WAVEfmt ")); $w.Write([int]16)
    $w.Write([int16]1); $w.Write([int16]2); $w.Write([int]$rate); $w.Write([int]($rate * 4))
    $w.Write([int16]4); $w.Write([int16]16)
    $w.Write([Text.Encoding]::ASCII.GetBytes("data")); $w.Write([int]$dataLen)
    for ($i = 0; $i -lt $n; $i++) {
        $w.Write([int16][Math]::Max(-32767, [Math]::Min(32767, $left[$i] * 32767)))
        $w.Write([int16][Math]::Max(-32767, [Math]::Min(32767, $right[$i] * 32767)))
    }
    $w.Flush(); [IO.File]::WriteAllBytes($path, $ms.ToArray())
}

# Bell note: fundamental + harmonics, short attack, exponential decay.
function Add-Bell([double[]]$buf, [double]$freq, [double]$start, [double]$dur, [double]$amp, [double]$decay) {
    $s0 = [int]($start * $rate); $len = [int]($dur * $rate)
    for ($i = 0; $i -lt $len -and ($s0 + $i) -lt $buf.Length; $i++) {
        $t = $i / $rate
        $env = [Math]::Min(1.0, $t / 0.005) * [Math]::Exp(-$t * $decay)
        $v = [Math]::Sin(2 * [Math]::PI * $freq * $t) + 0.4 * [Math]::Sin(2 * [Math]::PI * $freq * 2.01 * $t) + 0.15 * [Math]::Sin(2 * [Math]::PI * $freq * 3.02 * $t)
        $buf[$s0 + $i] += $amp * $env * $v / 1.55
    }
}

# Alert beep: softened square wave.
function Add-Beep([double[]]$buf, [double]$freq, [double]$start, [double]$dur, [double]$amp) {
    $s0 = [int]($start * $rate); $len = [int]($dur * $rate)
    for ($i = 0; $i -lt $len -and ($s0 + $i) -lt $buf.Length; $i++) {
        $t = $i / $rate
        $env = [Math]::Min(1.0, $t / 0.004) * [Math]::Min(1.0, ($dur - $t) / 0.02)
        $v = 0
        foreach ($k in 1, 3, 5) { $v += [Math]::Sin(2 * [Math]::PI * $freq * $k * $t) / $k }
        $buf[$s0 + $i] += $amp * $env * $v
    }
}

function New-Buf([double]$seconds) { New-Object double[] ([int]($seconds * $rate)) }

# Dawn: two rising, bright notes.
$b = New-Buf 1.6; Add-Bell $b 523.25 0 1.6 0.35 3; Add-Bell $b 783.99 0.25 1.35 0.35 3
Write-Wav "$OutDir\phase_dawn.wav" $b $b

# Day: one bright note.
$b = New-Buf 1.2; Add-Bell $b 880 0 1.2 0.3 4
Write-Wav "$OutDir\phase_day.wav" $b $b

# Evening: two falling notes (night is coming).
$b = New-Buf 1.8; Add-Bell $b 392 0 1.8 0.35 2.5; Add-Bell $b 293.66 0.3 1.5 0.35 2.5
Write-Wav "$OutDir\phase_evening.wav" $b $b

# Night: low bell, three strokes.
$b = New-Buf 3.2
Add-Bell $b 196 0 3.2 0.45 1.5; Add-Bell $b 146.83 0.6 2.6 0.45 1.5; Add-Bell $b 98 1.2 2.0 0.5 1.2
Write-Wav "$OutDir\phase_night.wav" $b $b

# Enemies: three levels, fully left or fully right.
# Level 1 (range): one low beep. Level 2 (half): two medium beeps. Level 3 (quarter): three fast high beeps.
$silence = New-Buf 0.6
$b = New-Buf 0.6; Add-Beep $b 440 0 0.16 0.3
Write-Wav "$OutDir\enemy_left_1.wav" $b $silence; Write-Wav "$OutDir\enemy_right_1.wav" $silence $b
$b = New-Buf 0.6; Add-Beep $b 660 0 0.1 0.33; Add-Beep $b 660 0.16 0.1 0.33
Write-Wav "$OutDir\enemy_left_2.wav" $b $silence; Write-Wav "$OutDir\enemy_right_2.wav" $silence $b
$b = New-Buf 0.6; Add-Beep $b 990 0 0.07 0.36; Add-Beep $b 990 0.11 0.07 0.36; Add-Beep $b 990 0.22 0.07 0.36
Write-Wav "$OutDir\enemy_left_3.wav" $b $silence; Write-Wav "$OutDir\enemy_right_3.wav" $silence $b
Remove-Item "$OutDir\enemy_left.wav", "$OutDir\enemy_right.wav" -ErrorAction SilentlyContinue

Get-ChildItem $OutDir -Filter *.wav | ForEach-Object { "{0,-20} {1,8} bytes" -f $_.Name, $_.Length }
