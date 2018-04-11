rgname=event-grid-rg
location=westus2
storageName=gridstoragesasha
binEndpoint=https://requestbin.fullcontact.com/q4a76lq4
gridSubName=gridBlobSasha


az group create --name $rgname --location $location

az storage account create \
  --name $storageName \
  --location $location \
  --resource-group $rgname \
  --sku Standard_LRS \
  --kind BlobStorage \
  --access-tier Hot
  
storageid=$(az storage account show --name $storageName --resource-group $rgname --query id --output tsv)

az eventgrid event-subscription create \
  --resource-id $storageid \
  --name $gridSubName \
  --endpoint $binEndpoint
  