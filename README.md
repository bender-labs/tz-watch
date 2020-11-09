# Setup local env

Install dotnet tools

restore local tools : `dotnet tool restore`

restore dependencies: `donet paket restore`

run project : `dotnet run --project TzWatch.Service `


# Test with curl

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