#pragma once

#include "..\common\rawaccel.hpp";
#include "wrapper.hpp";
using namespace rawaccel;
using namespace System;

	Tuple<double, double>^ ManagedAccel::Accelerate(Tuple<int, int> input, double time, double mode)
	{
		vec2d input_vec2d = {input.Item1, input.Item2};
		vec2d output = (*accel_instance)(input_vec2d, (accel_function::milliseconds)time, (rawaccel::mode)mode);

		return gcnew Tuple<double, double>(output.x, output.y);
	}
