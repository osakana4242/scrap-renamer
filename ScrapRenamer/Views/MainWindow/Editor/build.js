const fs = require("fs");
const path = require("path");
const esbuild = require("esbuild");

const sourceDirectory = path.join(__dirname, "dev");
const outputDirectory = path.join(__dirname, "min");

async function main() {
	fs.rmSync(outputDirectory, {
		recursive: true,
		force: true,
	});

	fs.cpSync(sourceDirectory, outputDirectory, {
		recursive: true,
	});	
	
	// js のコメント除去、 minify 化をする
	await esbuild.build({
		entryPoints: [
			path.join(sourceDirectory, "app.js"),
		],
		outfile: path.join(outputDirectory, "app.js"),
		bundle: false,
		minify: true,
	});

	// html と css のコメントを除去する
	const html = fs.readFileSync(
		path.join(sourceDirectory, "index.html"),
		"utf8",
	);

	const htmlWithoutComments = html.replace(
		/<!--[\s\S]*?-->/g,
		"",
	).replace(
		/\/\*[\s\S]*?\*\//g,
		"",
	);

	fs.writeFileSync(
		path.join(outputDirectory, "index.html"),
		htmlWithoutComments,
	);
}

main().catch(error => {
	console.error(error);
	process.exit(1);
});

