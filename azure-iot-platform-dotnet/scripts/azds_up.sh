if [ $1 == "usage" ]; then
    echo "Usage: azds_up.sh <service_name>"
    echo "Must be ran from repo root"
fi

cd src/services/$1/WebService && azds up