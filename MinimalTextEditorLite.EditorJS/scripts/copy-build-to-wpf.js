const fs = require("fs");
const path = require("path");

const editorRoot = path.resolve(__dirname, "..");
const repoRoot = path.resolve(editorRoot, "..");
const buildDir = path.join(editorRoot, "build");
const targetDir = path.join(repoRoot, "MinimalTextEditorLite.App", "EditorModules", "EditorJS");

function ensureDirectoryExists(directoryPath) {
  if (!fs.existsSync(directoryPath)) {
    fs.mkdirSync(directoryPath, { recursive: true });
  }
}

function removeDirectoryContents(directoryPath) {
  if (!fs.existsSync(directoryPath)) {
    return;
  }

  for (const entry of fs.readdirSync(directoryPath)) {
    const fullPath = path.join(directoryPath, entry);
    fs.rmSync(fullPath, { recursive: true, force: true });
  }
}

function copyRecursive(source, target) {
  const stat = fs.statSync(source);

  if (stat.isDirectory()) {
    ensureDirectoryExists(target);

    for (const entry of fs.readdirSync(source)) {
      copyRecursive(path.join(source, entry), path.join(target, entry));
    }

    return;
  }

  ensureDirectoryExists(path.dirname(target));
  fs.copyFileSync(source, target);
}

if (!fs.existsSync(buildDir)) {
  console.error(`[copy-build-to-wpf] Build folder not found: ${buildDir}`);
  console.error("[copy-build-to-wpf] Run npm run build first.");
  process.exit(1);
}

ensureDirectoryExists(targetDir);
removeDirectoryContents(targetDir);

for (const entry of fs.readdirSync(buildDir)) {
  copyRecursive(path.join(buildDir, entry), path.join(targetDir, entry));
}

console.log("[copy-build-to-wpf] React build copied to:");
console.log(targetDir);
