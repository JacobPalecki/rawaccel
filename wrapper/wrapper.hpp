#pragma once

#include "..\common\rawaccel.hpp";
using namespace rawaccel;
using namespace System;

public ref class ManagedAccel
{
protected:
	accel_function* accel_instance;
public:
	ManagedAccel(accel_function* accel)
		: accel_instance(accel)
	{
	}

    virtual ~ManagedAccel()
    {
        if (accel_instance != nullptr)
        {
            delete accel_instance;
        }
    }
    !ManagedAccel()
    {
        if (accel_instance != nullptr)
        {
            delete accel_instance;
        }
    }

    accel_function* GetInstance()
    {
        return accel_instance;
    }

    Tuple<double, double>^ Accelerate(Tuple<int, int> input, double time, double mode);
};
