require 'pp'

REQUIRED_XAMARIN_MAC_VERSION="2.0"
XAMARIN_MAC_VERSION_FILE="/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/Version"

REQUIRED_MONO_VERSION="4.0"
MONO_BINARY="/Library/Frameworks/Mono.Framework/Versions/Current/bin/mono"

class String
	def red;            "\e[31m#{self}\e[0m" end
end

def compare_version(first, second)
	val1 = first.split('.').map { |x| x.to_i }
	val2 = second.split('.').map { |x| x.to_i }

	return val1 <=> val2
end

def check_product(required_version, version_file, product_name)
	actual_version = File.read(version_file).strip
	retval = compare_version(actual_version, required_version)
	if (retval < 0)
		puts "Your installed #{product_name} (#{actual_version}) is too old, please use #{required_version} or newer".red
	end
	return retval
end

def check_mono(required_version, mono_binary)
	actual_version = `#{mono_binary} --version`
	# Extract the version number from a string like this:
	# 	`Mono JIT compiler version 4.2.0 (explicit/08b7103 Mon Aug 17 16:58:52 EDT 2015)`
	actual_version = actual_version.split('version ')[1]
	actual_version = actual_version.split(' ')[0]
	retval = compare_version(actual_version, required_version)
	if (retval < 0)
		puts "Your installed mono (#{actual_version}) is too old, please use #{required_version} or newer".red
	end
	return retval
end

def check_monodevelop_dependencies()
	mono_ret = check_mono(REQUIRED_MONO_VERSION, MONO_BINARY)
	xammac_ret = check_product(REQUIRED_XAMARIN_MAC_VERSION, XAMARIN_MAC_VERSION_FILE, "Xamarin.Mac")
	if (xammac_ret < 0 || mono_ret < 0)
		raise RuntimeError
	end
end

def run()
	check_monodevelop_dependencies()
end

$stdout.sync = true
run() if __FILE__==$0
