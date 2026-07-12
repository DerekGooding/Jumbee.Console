# Live Markdown

Type on the **left** — the preview on the **right** updates as you edit.
Try changing a heading, a list item, or a table cell.

## A table

| Control      | Kind        | Live |
|--------------|-------------|:----:|
| CodeEditor   | editor      |  ✎   |
| MarkdownView | viewer      |  👁  |
| SplitPanel   | layout      |  ↔   |

## Formatting

- **bold**, _italic_, and `inline code`
- nested lists:
  - first
  - second
- links like [Jumbee](https://example.com)

```csharp
var editor = new InteractiveMarkdownEditor("# Hi");
editor.TextChanged += text => Save(text);
```

> The render runs off the UI thread, so typing never blocks —
> a half-typed table just reflows on the next frame.
