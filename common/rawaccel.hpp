#pragma once

#define _USE_MATH_DEFINES
#include <math.h>

#include "x64-util.hpp"
#include "external/tagged-union-single.h"

#include "accel-linear.hpp"
#include "accel-classic.hpp"
#include "accel-natural.hpp"
#include "accel-logarithmic.hpp"
#include "accel-sigmoid.hpp"
#include "accel-power.hpp"
#include "accel-noaccel.hpp"

namespace rawaccel {

    /// <summary> Struct to hold vector rotation details. </summary>
    struct rotator {

        /// <summary> Rotational vector, which points in the direction of the post-rotation positive x axis. </summary>
        vec2d rot_vec = { 1, 0 };

        /// <summary>
        /// Rotates given input vector according to struct's rotational vector.
        /// </summary>
        /// <param name="input">Input vector to be rotated</param>
        /// <returns>2d vector of rotated input.</returns>
        inline vec2d operator()(const vec2d& input) const {
            return {
                input.x * rot_vec.x - input.y * rot_vec.y,
                input.x * rot_vec.y + input.y * rot_vec.x
            };
        }

        rotator(double degrees) {
            double rads = degrees * M_PI / 180;
            rot_vec = { cos(rads), sin(rads) };
        }

        rotator() = default;
    };

    /// <summary> Struct to hold clamp (min and max) details for acceleration application </summary>
    struct accel_scale_clamp {
        double lo = 0;
        double hi = 9000;

        /// <summary>
        /// Clamps given input to min at lo, max at hi.
        /// </summary>
        /// <param name="scale">Double to be clamped</param>
        /// <returns>Clamped input as double</returns>
        inline double operator()(double scale) const {
            return clampsd(scale, lo, hi);
        }

        accel_scale_clamp(double cap) : accel_scale_clamp() {
            if (cap <= 0) {
                // use default, effectively uncapped accel
                return;
            }

            if (cap < 1) {
                // assume negative accel
                lo = cap;
                hi = 1;
            }
            else hi = cap;
        }

        accel_scale_clamp() = default;
    };

    /// <summary> Tagged union to hold all accel implementations and allow "polymorphism" via a visitor call. </summary>
    using accel_impl_t = tagged_union<accel_linear, accel_classic, accel_natural, accel_logarithmic, accel_sigmoid, accel_power, accel_noaccel>;

    struct accel_fn_args {
        accel_args acc_args;
        int accel_mode = accel_impl_t::id<accel_noaccel>;
        milliseconds time_min = 0.4;
        vec2d cap = { 0, 0 };
    };

    /// <summary> Struct for holding acceleration application details. </summary>
    struct accel_function {

        /*
        This value is ideally a few microseconds lower than
        the user's mouse polling interval, though it should
        not matter if the system is stable.
        */
        /// <summary> The minimum time period for one mouse movement. </summary>
        milliseconds time_min = 0.4;

        /// <summary> The offset past which acceleration is applied. </summary>
        double speed_offset = 0;

        /// <summary> The acceleration implementation (i.e. curve) </summary>
        accel_impl_t accel;

        /// <summary> The object which sets a min and max for the acceleration scale. </summary>
        vec2<accel_scale_clamp> clamp;

        double cap_slope;
        
        double cap_intercept;

        accel_function(const accel_fn_args& args) {
            if (args.time_min <= 0) error("min time must be positive");
            if (args.acc_args.offset < 0) error("offset must not be negative");

            accel.tag = args.accel_mode;
            accel.visit([&](auto& impl){ impl = { args.acc_args }; });

            time_min = args.time_min;
            speed_offset = args.acc_args.offset;

            if (args.cap.x > 0 && args.cap.y > 0)
            {
				double mag_1_in = args.cap.x;
				vec2d mag_1_out_vec = accel.visit([=](auto&& impl) {
					double accel_val = impl.accelerate(mag_1_in);
					return impl.scale(accel_val); 
				});
				double mag_1_out = mag_1_out_vec.x*mag_1_in;

                double mag_2_in = args.cap.x * 1.01;
				vec2d mag_2_out_vec = accel.visit([=](auto&& impl) {
					double accel_val = impl.accelerate(mag_2_in);
					return impl.scale(accel_val); 
				});
                double mag_2_out = mag_2_out_vec.x*mag_2_in;
                cap_slope = (mag_2_out - mag_1_out) / (mag_2_in - mag_1_in);
                cap_intercept = mag_1_out - cap_slope * mag_1_in;
            }

            clamp.x = accel_scale_clamp(args.cap.x);
            clamp.y = accel_scale_clamp(args.cap.y);
        }

        /// <summary>
        /// Applies weighted acceleration to given input for given time period.
        /// </summary>
        /// <param name="input">2d vector of {x, y} mouse movement to be accelerated</param>
        /// <param name="time">Time period over which input movement was accumulated</param>
        /// <returns></returns>
        inline vec2d operator()(const vec2d& input, milliseconds time) const {
            double mag = sqrtsd(input.x * input.x + input.y * input.y);
            double time_clamped = clampsd(time, time_min, 100);
            double speed = maxsd(mag / time_clamped - speed_offset, 0);

            vec2d scale = accel.visit([=](auto&& impl) {
                double accel_val = impl.accelerate(speed);
                return impl.scale(accel_val); 
            });

            vec2d output = {
                input.x * scale.x,
                input.y * scale.y
            };

            if (speed >= clamp.x.hi)
            {
                double out_mult = cap_slope + cap_intercept / speed;
                output.x = out_mult * input.x;
                output.y = out_mult * input.y;
            }

            return output;
        }

        accel_function() = default;
    };

    struct modifier_args {
        double degrees = 0;
        vec2d sens = { 1, 1 };
        accel_fn_args acc_fn_args;
    };

    /// <summary> Struct to hold variables and methods for modifying mouse input </summary>
    struct mouse_modifier {
        bool apply_rotate = false;
        bool apply_accel = false;
        rotator rotate;
        accel_function accel_fn;
        vec2d sensitivity = { 1, 1 };

        mouse_modifier(const modifier_args& args)
            : accel_fn(args.acc_fn_args)
        {
            apply_rotate = args.degrees != 0;

            if (apply_rotate) rotate = rotator(args.degrees);
            else rotate = rotator();

            apply_accel = args.acc_fn_args.accel_mode != 0 &&
                args.acc_fn_args.accel_mode != accel_impl_t::id<accel_noaccel>;

            if (args.sens.x == 0) sensitivity.x = 1;
            else sensitivity.x = args.sens.x;

            if (args.sens.y == 0) sensitivity.y = 1;
            else sensitivity.y = args.sens.y;
        }

        /// <summary>
        /// Applies modification without acceleration.
        /// </summary>
        /// <param name="input">Input to be modified.</param>
        /// <returns>2d vector of modified input.</returns>
        inline vec2d modify_without_accel(vec2d input)
        {
            if (apply_rotate)
            {
                input = rotate(input);
            }

            input.x *= sensitivity.x;
            input.y *= sensitivity.y;

            return input;
        }

        /// <summary>
        /// Applies modification, including acceleration.
        /// </summary>
        /// <param name="input">Input to be modified</param>
        /// <param name="time">Time period for determining acceleration.</param>
        /// <returns>2d vector with modified input.</returns>
        inline vec2d modify_with_accel(vec2d input, milliseconds time)
        {
            if (apply_rotate)
            {
                input = rotate(input);
            }

			input = accel_fn(input, time);

            input.x *= sensitivity.x;
            input.y *= sensitivity.y;

            return input;
        }

        mouse_modifier() = default;
    };

} // rawaccel