.DEFAULT_GOAL 	:= default

ARTIFACTS 		:= $(shell pwd)/artifacts
BUILD			:= $(shell pwd)/.build

.PHONY: default
default: package

.PHONY: clean
clean:
	@rm -rf $(ARTIFACTS)
	@rm -rf $(BUILD)


.PHONY: generate.index
generate.index:
	helm repo index --url https://vandycknick.github.io/cloudflare-ddns/ .


