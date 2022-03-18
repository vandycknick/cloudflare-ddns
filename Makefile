.DEFAULT_GOAL 		:= default

ARTIFACTS 			:= $(shell pwd)/artifacts
BUILD				:= $(shell pwd)/.build
CONFIGURATION		:= Release
CLI_PROJECT			:= src/cloudflare-ddns/cloudflare-ddns.csproj
CLI_TEST_PROJECT	:= test/CloudflareDDNS.Tests/CloudflareDDNS.Tests.csproj
CLI_TOOL			:= cloudflare-ddns
RUNTIME 			:= $(shell uname -s | awk '{print tolower($$0)}' | sed "s/darwin/osx/")-x64

.PHONY: default
default: package

.PHONY: clean
clean:
	@rm -rf $(ARTIFACTS)
	@rm -rf $(BUILD)

.PHONY: restore
restore:
	dotnet restore
	dotnet tool restore

.PHONY: restore
build: restore
	dotnet build --configuration $(CONFIGURATION) --no-restore $(CLI_PROJECT)

.PHONY: build.docker
build.docker: VERSION:=$(shell dotnet minver -t v -a minor -v e)
build.docker:
	echo $(VERSION)
	cd $(shell dirname $(CLI_PROJECT)) && \
		docker build --build-arg MINVERVERSIONOVERRIDE=$(VERSION) -t cloudflare-ddns:v$(VERSION) .

.PHONY: version
version:
	@dotnet minver -t v -a minor -v e

.PHONY: run
run:
	dotnet run --project $(CLI_PROJECT) -- --log-level verbose

.PHONY: test
test:
	dotnet test $(CLI_TEST_PROJECT) /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

.PHONY: package
package: restore build
	@echo ""
	@echo "\033[0;32mPackaging nuget \033[0m"
	@echo "\033[0;32m------------------- \033[0m"

	dotnet pack $(CLI_PROJECT) --configuration $(CONFIGURATION) \
		--no-build \
		--output $(ARTIFACTS) \
		--include-symbols

.PHONY: package.chart
package.chart:
	helm package charts/cloudflare-ddns --destination .build
