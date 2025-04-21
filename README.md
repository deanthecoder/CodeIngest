[![Twitter URL](https://img.shields.io/twitter/url/https/twitter.com/deanthecoder.svg?style=social&label=Follow%20%40deanthecoder)](https://twitter.com/deanthecoder)
# CodeIngest
**CodeIngest** is a cross-platform C# CLI tool that recursively scans a directory of source files, filters out noise (comments, using statements, namespaces), and generates a flattened source dump designed for GPT code review or large-scale source inspection.

## Features
- Cross-platform (.NET)
- Strips comments, `using` directives, and `namespace` blocks
- Outputs a single readable output file with the following features:
  - File headers
  - Line numbers
  - Cleaned source code
- Skips generated and irrelevant files (e.g. `.designer.cs`, `bin`, `obj`, `.resx`, etc.)

## Usage
```
CodeIngest <directory> [<directory> ...] [*.ext1;*.ext2] <output.code>
```

### Examples
```
CodeIngest MyProject Out.cs
CodeIngest Src1 Src2 *.cs;*.txt Dump.txt
CodeIngest *.cs;*.cpp SourceDump.code
```

## Example Output
```csharp
// CodeIngest Source Dump - A CLI tool that merges and processes code files for GPT reviews.
// Notes: Some code content may have been removed.
// File: GameState.cs
20|public class GameState : IAiGameState
21|{
22|private readonly Vector2[] m_bats;
23|private readonly Vector2 m_ballPosition;
24|private readonly Vector2 m_ballVelocity;
25|private readonly int m_arenaWidth;
26|private readonly int m_arenaHeight;
28|public GameState(Vector2[] bats, Vector2 ballPosition, Vector2 ballVelocity, int arenaWidth, int arenaHeight)
29|{
30|m_bats = bats;
31|m_ballPosition = ballPosition;
32|m_ballVelocity = ballVelocity;
33|m_arenaWidth = arenaWidth;
34|m_arenaHeight = arenaHeight;
35|}
37|public double[] ToInputVector()
38|{
39|var inputVector = new double[Brain.BrainInputCount];
42|inputVector[0] = m_bats[0].Y / m_arenaHeight * 2.0f - 1.0f;
43|inputVector[1] = m_bats[1].Y / m_arenaHeight * 2.0f - 1.0f;
45|inputVector[2] = (m_bats[0].Y - m_ballPosition.Y) / m_arenaHeight * 2.0f;
46|inputVector[3] = (m_bats[1].Y - m_ballPosition.Y) / m_arenaHeight * 2.0f;
49|inputVector[4] = m_ballPosition.X / m_arenaWidth * 2.0f - 1.0f;
50|inputVector[5] = m_ballPosition.Y / m_arenaHeight * 2.0f - 1.0f;
51|inputVector[6] = m_ballVelocity.X.Clamp(-1.0f, 1.0f);
52|inputVector[7] = m_ballVelocity.Y.Clamp(-1.0f, 1.0f);
54|return inputVector;
55|}
56|}
// File: Brain.cs
18|public class Brain : AiBrainBase
19|{
20|public const int BrainInputCount = 8;
22|public Brain() : base(BrainInputCount, [16], 4)
23|{
24|}
26|private Brain(Brain brain) : base(brain)
27|{
28|}
29|public (Direction LeftBat, Direction RightBat) ChooseMoves(IAiGameState state)
30|{
31|var outputs = GetOutputs(state);
33|var leftBatDirection = Direction.Left;
34|var diff = outputs[0] - outputs[1];
35|if (Math.Abs(diff) > 0.2)
36|leftBatDirection = diff > 0 ? Direction.Up : Direction.Down;
38|var rightBatDirection = Direction.Left;
39|diff = outputs[2] - outputs[3];
40|if (Math.Abs(diff) > 0.2)
41|rightBatDirection = diff > 0 ? Direction.Up : Direction.Down;
43|return (leftBatDirection, rightBatDirection);
44|}
46|public override AiBrainBase Clone() => new Brain(this);
47|}
```
