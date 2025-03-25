#/bin/bash

if [ ! -d ".git" ]; then
    echo "This script must be run in the root of the repository."
    exit 1
fi

VERSION=$(.github/get-version.sh $@)

jq ".version=\"$VERSION\"" package.json > package.json.tmp
mv package.json.tmp package.json

echo "Updated package version to $VERSION"
