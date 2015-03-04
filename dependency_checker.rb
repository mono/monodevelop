require 'pp'

REQUIRED_XAMARIN_MAC_VERSION="1.12.0.4"
XAMARIN_MAC_VERSION_FILE="/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/Version"

def compare_version(first, second)
	val1 = first.split('.').map { |x| x.to_i }
	val2 = second.split('.').map { |x| x.to_i }

	return val1 <=> val2
end

def check_product(required_version, version_file, product_name)
	actual_version = File.read(version_file).strip
	retval = compare_version(actual_version, required_version)
	if (retval < 0)
		puts "Your installed #{product_name} (#{actual_version}) is too old, please use #{required_version} or newer"
	end
	return retval
end

def run()
	xammac_ret = check_product(REQUIRED_XAMARIN_MAC_VERSION, XAMARIN_MAC_VERSION_FILE, "Xamarin.Mac")
	if (xammac_ret < 0)
		raise RuntimeError
	end
end

$stdout.sync = true
run()
