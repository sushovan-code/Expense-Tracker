# Cloud deployment view with official Azure icons.
# Run via scripts/render-diagrams.ps1; writes deployment-azure.png next to this file.
# Claude rewrites the nodes from infra/ Terraform via /update-diagrams.
from diagrams import Cluster, Diagram, Edge
from diagrams.azure.compute import ContainerApps
from diagrams.azure.database import SQLDatabases
from diagrams.azure.web import StaticApps
from diagrams.onprem.client import Users
from diagrams.onprem.inmemory import Redis

graph_attr = {"bgcolor": "white", "pad": "0.4", "fontname": "Segoe UI"}

with Diagram("<Product> on Azure", filename="deployment-azure", outformat="png",
             show=False, direction="LR", graph_attr=graph_attr):
    users = Users("Users")
    with Cluster("rg-<product>-dev"):
        web = StaticApps("Static Web App")
        api = ContainerApps("Container App: api")
        sql = SQLDatabases("Azure SQL (free)")
    cache = Redis("Upstash Redis")
    users >> web >> Edge(label="HTTPS") >> api
    api >> sql
    api >> cache
