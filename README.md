Watcher for smart contracts on Tezos. 

It can be run as a standalone webserver, using SSE, or be included as a lib. 

# Setup local env

Install dotnet tools

restore local tools : `dotnet tool restore`

restore dependencies: `donet paket restore`


# WebServer 

Run : `run project : `dotnet run --project TzWatch.Http`

## Test web server with with curl

Sample payload: 
```shell
PAYLOAD=`cat <<EOF
{
    "address":"KT1SoJnh2jc2ChoBJwEGgZQyUVvRzPNmztQX",
    "level":179220,
    "confirmations":3,
    "interests":[
        {"type":"entrypoint", "value":"burn"},
        {"type":"entrypoint", "value":"mint"},
        {"type":"entrypoint", "value":"transfer"}]
}
EOF`
```

invoke : `curl -N -X POST -d $PAYLOAD http://localhost:5000/subscriptions`

# Distribution

For now, tz-watch is published on github packages.
Follow [this instructions](https://docs.github.com/en/free-pro-team@latest/packages/using-github-packages-with-your-projects-ecosystem/configuring-dotnet-cli-for-use-with-github-packages#authenticating-to-github-packages) to add bender-labs as a nuget source.

## Build and publish

`dotnet pack -c Release --include-symbols -o nugets -p:PackageVersion="0.0.4"`

`dotnet nuget push nugets/Nichelson.{version}.nupkg --source <the source you configured>`%