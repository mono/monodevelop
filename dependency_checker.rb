require 'pp'

XAMARIN_MAC_MIN_VERSION="2.3"
XAMARIN_MAC_VERSION=lambda { product_version ("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/bin/mmp") }
XAMARIN_MAC_URL="http://storage.bos.internalx.com/macios-mac-cycle6/1e/1e6896dd96bc9387725b4332175baace1bef8186/xamarin.mac-2.3.0.135.pkg"

MONO_MIN_VERSION="4.2"
MONO_VERSION=lambda { mono_version("/Library/Frameworks/Mono.framework/Versions/Current/bin/mono") }
MONO_URL="http://storage.bos.internalx.com/mono-mac-4.2.0-branch/27/2701b194139f851f54660bd66c97074b041427fd/MonoFramework-MDK-4.2.0.207.macos10.xamarin.x86.pkg"

class String
	def red;            "\e[31m#{self}\e[0m" end
end

def compare_version(first, second)
	val1 = first.split('.').map { |x| x.to_i }
	val2 = second.split('.').map { |x| x.to_i }

	return val1 <=> val2
end

def mono_version(binary)
	actual_version = `#{binary} --version`
	# Extract the version number from a string like this:
	# 	`Mono JIT compiler version 4.2.0 (explicit/08b7103 Mon Aug 17 16:58:52 EDT 2015)`
	actual_version = actual_version.split('version ')[1]
	return actual_version.split(' ')[0]
end

def product_version(binary)
	version = `#{binary} --version`
	return version.split(' ')[1]
end

def check_product(product_min_version, product_version, product_url, product_name)
	actual_version = product_version.call
	retval = compare_version(actual_version, product_min_version)
	if (retval < 0)
		puts ""
		puts "Your installed #{product_name} (#{actual_version}) is too old, please use #{product_min_version} or newer".red
		puts "You can download it from #{product_url}".red
	end
	return retval
end

def check_monodevelop_dependencies()
	result = [
		check_product(MONO_MIN_VERSION, MONO_VERSION, MONO_URL, "Mono"),
		check_product(XAMARIN_MAC_MIN_VERSION, XAMARIN_MAC_VERSION, XAMARIN_MAC_URL, "Xamarin.Mac")
	]
	if (result.min < 0)
		raise RuntimeError
	end
end

$stdout.sync = true
check_monodevelop_dependencies() if __FILE__==$0