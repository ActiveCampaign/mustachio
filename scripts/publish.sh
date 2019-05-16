#!/usr/bin/env bash
set -e
shopt -s expand_aliases

dotnet restore

#generate a version, based on the tag, or rev.
revision=$(git rev-parse HEAD)
inferred_assembly_version="2.0.0.${TRAVIS_BUILD_NUMBER:-0}"
version=$(echo "${TRAVIS_TAG:-$inferred_assembly_version}" | sed -E s/-.+$//)
descriptive_version=${TRAVIS_TAG:-"$version-git-${revision:0:6}"}

# build and test in one step.
dotnet pack -c Release -o ../tmp ./Mustachio/Mustachio.csproj -p:AssemblyVersion=$version -p:PackageVersion=$descriptive_version

package_path="../tmp/Postmark.$descriptive_version.nupkg"

echo "The package path is located at: $package_path"
echo 'Here is what is located at that path:'
ls ./tmp

if [[ $TRAVIS_TAG && $NUGET_API_KEY && $MYGET_API_KEY ]]; then
    echo 'This package will be published to NuGet, and MyGet'
    dotnet nuget push $package_path -s https://www.nuget.org/ -k $NUGET_API_KEY
    dotnet nuget push $package_path -s https://www.myget.org/F/postmark-ci/api/v3/index.json -k $MYGET_API_KEY
else
    echo 'This package will be published to the CI "MyGet" feed:'
    dotnet nuget push $package_path -s https://www.myget.org/F/postmark-ci/api/v3/index.json -k $MYGET_API_KEY
fi