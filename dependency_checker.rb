require 'pp'

NOT_INSTALLED_VERSION="-1"

XAMARIN_MAC_MIN_VERSION="2.3"
XAMARIN_MAC_VERSION=lambda { product_version ("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/bin/mmp") }
XAMARIN_MAC_URL="http://www.xamarin.com"

MONO_MIN_VERSION="4.2"
MONO_VERSION=lambda { mono_version("/Library/Frameworks/Mono.framework/Versions/Current/bin/mono") }
MONO_URL="http://mono-project.com"

class String
	def red;            "\e[31m#{self}\e[0m" end
end

def compare_version(first, second)
	val1 = first.split('.').map { |x| x.to_i }
	val2 = second.split('.').map { |x| x.to_i }

	return val1 <=> val2
end

def mono_version(binary)
	if File.exist?("#{binary}")
		actual_version = `#{binary} --version`
		# Extract the version number from a string like this:
		# 	`Mono JIT compiler version 4.2.0 (explicit/08b7103 Mon Aug 17 16:58:52 EDT 2015)`
		actual_version = actual_version.split('version ')[1]
		return actual_version.split(' ')[0]
	else
		return NOT_INSTALLED_VERSION
	end
end

def product_version(binary)
	if File.exist?("#{binary}")
		version = `#{binary} --version`
		return version.split(' ')[1]
	else
		return NOT_INSTALLED_VERSION
	end
end

def check_product(product_min_version, product_version, product_url, product_name)
	actual_version = product_version.call
	retval = compare_version(actual_version, product_min_version)

	if (retval < 0)
		if (actual_version == NOT_INSTALLED_VERSION)
			puts "You do not have #{product_name} installed.".red
		else
			puts "Your installed #{product_name} (#{actual_version}) is too old, please use #{product_min_version} or newer".red
		end
		puts "You can download it from #{product_url}".red
		puts
	end
	return retval
end

def check_monodevelop_dependencies()
	result = [
		check_product(MONO_MIN_VERSION, MONO_VERSION, MONO_URL, "Mono"),
		check_product(XAMARIN_MAC_MIN_VERSION, XAMARIN_MAC_VERSION, XAMARIN_MAC_URL, "Xamarin.Mac")
	]
	if (result.min < 0)
		exit 1
	end
end

$stdout.sync = true
check_monodevelop_dependencies() if __FILE__==$0
